using UnaryExpression = System.Linq.Expressions.UnaryExpression;
using BinaryExpression = System.Linq.Expressions.BinaryExpression;

// Some operators only apply to the entities returned by a query: EF only applies Include to them, and only tracks
// them. When the query returns no entity, for example since it ends in a projection or a scalar like Count, EF
// silently ignores those operators.
// The checks lean towards not throwing: an operator that might return an entity is assumed to return one.
static class IgnoredEntityOperatorDetector
{
    public static void ThrowIfIgnored(Expression query, IModel model)
    {
        // the operators applied to the query, from the root outwards
        var calls = new List<MethodCallExpression>();
        var expression = query;
        while (expression is
               MethodCallExpression
               {
                   Method.IsStatic: true,
                   Arguments.Count: > 0
               } call)
        {
            calls.Add(call);
            expression = call.Arguments[0];
        }

        calls.Reverse();

        var returnsEntities = IsEntity(expression.Type, model);
        MethodCallExpression? lostBy = null;
        var pending = new List<MethodCallExpression>();
        foreach (var call in calls)
        {
            if (IsEntityOperator(call.Method))
            {
                if (!returnsEntities)
                {
                    throw Ignored([call], AfterReason(lostBy));
                }

                pending.Add(call);
                continue;
            }

            returnsEntities = ReturnsEntities(call, returnsEntities, model);
            if (returnsEntities)
            {
                continue;
            }

            lostBy = call;
            if (pending.Count > 0)
            {
                throw Ignored(pending, $"it is followed by {call.Method.Name}, which returns no entity");
            }
        }
    }

    static string AfterReason(MethodCallExpression? lostBy)
    {
        if (lostBy == null)
        {
            return "the query returns no entity";
        }

        return $"it comes after {lostBy.Method.Name}, which returns no entity";
    }

    static Exception Ignored(List<MethodCallExpression> calls, string reason)
    {
        var builder = new StringBuilder();
        builder.Append($"{string.Join('.', calls.Select(_ => _.Describe()))} is ignored, since {reason}.");
        if (calls.Any(_ => IsInclude(_.Method)))
        {
            builder.Append(
                """

                EF only applies Include to entities returned by the query, and a projection already loads the related data it references.
                """);
        }

        if (calls.Any(_ => IsTracking(_.Method)))
        {
            builder.Append(
                """

                EF only tracks entities, so tracking options do nothing on a query that returns none.
                """);
        }

        builder.Append(
            """

            Remove it.
            """);
        return new(builder.ToString());
    }

    static bool IsEntityOperator(MethodInfo method) =>
        IsInclude(method) ||
        IsTracking(method);

    static bool IsInclude(MethodInfo method) =>
        method.DeclaringType == typeof(EntityFrameworkQueryableExtensions) &&
        method.Name is
            nameof(EntityFrameworkQueryableExtensions.Include) or
            nameof(EntityFrameworkQueryableExtensions.ThenInclude);

    static bool IsTracking(MethodInfo method) =>
        method.DeclaringType == typeof(EntityFrameworkQueryableExtensions) &&
        method.Name is
            nameof(EntityFrameworkQueryableExtensions.AsNoTracking) or
            nameof(EntityFrameworkQueryableExtensions.AsNoTrackingWithIdentityResolution) or
            nameof(EntityFrameworkQueryableExtensions.AsTracking);

    static bool ReturnsEntities(MethodCallExpression call, bool sourceReturnsEntities, IModel model)
    {
        var arguments = call.Arguments;
        // operators like Where, OrderBy, Take, and First return elements of the source
        if (ElementOrSelf(call.Type) == ElementOrSelf(arguments[0].Type))
        {
            return sourceReturnsEntities;
        }

        if (IsEntity(call.Type, model))
        {
            return true;
        }

        // a result selector, for example of Select or Join, can return an entity inside a new type
        var selector = arguments
            .Skip(1)
            .Select(_ => _.Unquote())
            .OfType<LambdaExpression>()
            .LastOrDefault();
        if (selector == null)
        {
            return false;
        }

        return EntityFinder.Find(selector.Body, model);
    }

    // An entity type, a type an entity can be assigned to (for example object or an interface),
    // or a collection of either.
    static bool IsEntity(Type type, IModel model)
    {
        if (type == typeof(string))
        {
            return false;
        }

        if (model.GetEntityTypes().Any(_ => type.IsAssignableFrom(_.ClrType)))
        {
            return true;
        }

        var element = ElementType(type);
        return element != null &&
               IsEntity(element, model);
    }

    static Type ElementOrSelf(Type type) =>
        ElementType(type) ?? type;

    static Type? ElementType(Type type)
    {
        if (type == typeof(string))
        {
            return null;
        }

        if (IsEnumerable(type))
        {
            return type.GetGenericArguments()[0];
        }

        return type
            .GetInterfaces()
            .FirstOrDefault(IsEnumerable)
            ?.GetGenericArguments()[0];
    }

    static bool IsEnumerable(Type type) =>
        type.IsGenericType &&
        type.GetGenericTypeDefinition() == typeof(IEnumerable<>);

    // Finds an entity that is placed in the result, as opposed to an entity that is only used to reach a member
    // (_.Name), compared (_.Company == null), or queried (_.Employees.Count()).
    class EntityFinder(IModel model) :
        ExpressionVisitor
    {
        bool found;

        public static bool Find(Expression expression, IModel model)
        {
            var finder = new EntityFinder(model);
            finder.Visit(expression);
            return finder.found;
        }

        [return: NotNullIfNotNull(nameof(node))]
        public override Expression? Visit(Expression? node)
        {
            node = Unconvert(node);
            if (found ||
                node == null)
            {
                return node;
            }

            if (IsEntity(node.Type, model))
            {
                found = true;
                return node;
            }

            return base.Visit(node);
        }

        // the parameters of a lambda are not part of its result
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            Visit(node.Body);
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            VisitUsed(node.Expression);
            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var type = node.Method.DeclaringType;
            if (node.Method.IsStatic &&
                node.Arguments.Count > 0 &&
                (type == typeof(Queryable) ||
                 type == typeof(Enumerable) ||
                 type == typeof(EF)))
            {
                VisitUsed(node.Arguments[0]);
                foreach (var argument in node.Arguments.Skip(1))
                {
                    Visit(argument);
                }

                return node;
            }

            return base.VisitMethodCall(node);
        }

        // comparing entities compares their keys
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
            {
                VisitUsed(node.Left);
                VisitUsed(node.Right);
                return node;
            }

            return base.VisitBinary(node);
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            VisitUsed(node.Expression);
            return node;
        }

        // visits the children of an expression, without treating the expression itself as part of the result
        void VisitUsed(Expression? node)
        {
            node = Unconvert(node);
            if (found ||
                node == null)
            {
                return;
            }

            base.Visit(node);
        }

        static Expression? Unconvert(Expression? node)
        {
            while (node is UnaryExpression
                   {
                       NodeType:
                       ExpressionType.Convert or
                       ExpressionType.ConvertChecked or
                       ExpressionType.TypeAs
                   } unary)
            {
                node = unary.Operand;
            }

            return node;
        }
    }
}
