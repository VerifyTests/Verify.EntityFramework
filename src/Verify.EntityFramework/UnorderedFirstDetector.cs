// `_.Employees.FirstOrDefault()` in a projection or a predicate is translated to a subquery with TOP(1) and no ORDER BY,
// so it returns an arbitrary employee. EF logs FirstWithoutOrderByAndFilterWarning for First without an ordering or a
// filter, but counts the correlation of a navigation with its parent as a filter, so never logs it for a navigation:
// https://github.com/dotnet/efcore/issues/39129. First on the root of the query is left to EF.
// The check leans towards not throwing: only a navigation followed by operators that keep its elements is matched, so
// any Where, ordering, or row limit is assumed to choose the element.
class UnorderedFirstDetector(IModel model) :
    ExpressionVisitor
{
    int lambdaDepth;

    public static void ThrowIfUnordered(Expression query, IModel model) =>
        new UnorderedFirstDetector(model).Visit(query);

    // only a query inside a lambda is a subquery
    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        lambdaDepth++;
        base.VisitLambda(node);
        lambdaDepth--;
        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // the overloads with a predicate are filtered
        if (lambdaDepth > 0 &&
            IsFirst(node.Method) &&
            node.Arguments.Count == 1 &&
            UnorderedNavigation(node.Arguments[0]) is { } navigation)
        {
            throw new(
                $"""
                 `{node}` returns an arbitrary element of {navigation.Member.Name}, since EF translates it to a subquery that takes the first row without an ORDER BY.
                 Add an OrderBy before {node.Method.Name}.
                 """);
        }

        return base.VisitMethodCall(node);
    }

    static bool IsFirst(MethodInfo method) =>
        IsLinq(method) &&
        method.Name is
            nameof(Queryable.First) or
            nameof(Queryable.FirstOrDefault);

    // the collection navigation the source reads, when nothing between them orders, filters, or limits it
    MemberExpression? UnorderedNavigation(Expression source)
    {
        while (true)
        {
            switch (source)
            {
                case MethodCallExpression
                {
                    Method.IsStatic: true,
                    Arguments.Count: > 0
                } call when KeepsElements(call.Method):
                    source = call.Arguments[0];
                    continue;
                case MemberExpression member when IsCollectionNavigation(member.Type):
                    return member;
                default:
                    return null;
            }
        }
    }

    static bool KeepsElements(MethodInfo method) =>
        IsLinq(method) &&
        method.Name is
            nameof(Queryable.Select) or
            nameof(Queryable.Distinct) or
            nameof(Queryable.Cast) or
            nameof(Queryable.AsQueryable) or
            nameof(Enumerable.AsEnumerable);

    // owned collections are not matched, since EF orders them by their key
    bool IsCollectionNavigation(Type type)
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
        if (enumerable == null)
        {
            return false;
        }

        var entityType = model.FindEntityType(enumerable.GetGenericArguments()[0]);
        return entityType != null &&
               !entityType.IsOwned();
    }

    static bool IsLinq(MethodInfo method) =>
        method.DeclaringType == typeof(Queryable) ||
        method.DeclaringType == typeof(Enumerable);
}
