// AsSplitQuery and AsSingleQuery only change how collections are loaded, by a collection Include or a collection in
// a projection. A query that loads no collection is a single query either way. A single collection is enough for
// AsSplitQuery to have an effect, since it then avoids repeating the parent columns for each child row.
// The check leans towards not throwing: any collection in a lambda, even one only used to filter, counts as loaded.
static class IgnoredQuerySplittingDetector
{
    public static void ThrowIfIgnored(Expression query, IModel model)
    {
        MethodCallExpression? splitting = null;
        var finder = new CollectionFinder(model);
        var expression = query;
        while (expression is
               MethodCallExpression
               {
                   Method.IsStatic: true,
                   Arguments.Count: > 0
               } call)
        {
            if (IsSplitting(call.Method))
            {
                splitting ??= call;
            }
            // a string Include can not be checked without resolving the path, so it is assumed to be a collection
            else if (IsInclude(call.Method) &&
                     call.Arguments[1].Unquote() is not LambdaExpression)
            {
                return;
            }
            else
            {
                foreach (var argument in call.Arguments.Skip(1))
                {
                    finder.Visit(argument.Unquote());
                }
            }

            if (finder.Found)
            {
                return;
            }

            expression = call.Arguments[0];
        }

        if (splitting == null)
        {
            return;
        }

        throw new(
            $"""
             {splitting.Describe()} is ignored, since the query loads no collection.
             Query splitting only changes how collection Includes and collections in a projection are loaded.
             Remove it.
             """);
    }

    static bool IsSplitting(MethodInfo method) =>
        method.DeclaringType == typeof(RelationalQueryableExtensions) &&
        method.Name is
            nameof(RelationalQueryableExtensions.AsSplitQuery) or
            nameof(RelationalQueryableExtensions.AsSingleQuery);

    static bool IsInclude(MethodInfo method) =>
        method.DeclaringType == typeof(EntityFrameworkQueryableExtensions) &&
        method.Name is
            nameof(EntityFrameworkQueryableExtensions.Include) or
            nameof(EntityFrameworkQueryableExtensions.ThenInclude);

    // finds an expression that is a collection of entities, for example _.Employees or _.Employees.Where(...)
    class CollectionFinder(IModel model) :
        ExpressionVisitor
    {
        public bool Found { get; private set; }

        public override Expression? Visit(Expression? node)
        {
            if (Found ||
                node == null)
            {
                return node;
            }

            if (IsEntityCollection(node.Type))
            {
                Found = true;
                return node;
            }

            return base.Visit(node);
        }

        // the parameters of a lambda are single entities, not collections
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            Visit(node.Body);
            return node;
        }

        bool IsEntityCollection(Type type)
        {
            if (type == typeof(string))
            {
                return false;
            }

            var enumerable = type
                .GetInterfaces()
                .Append(type)
                .FirstOrDefault(_ => _.IsGenericType &&
                                     _.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            return enumerable != null &&
                   model.FindEntityType(enumerable.GetGenericArguments()[0]) != null;
        }
    }
}
