// An OrderBy replaces any earlier ordering, unless a row limiting operator, like Take, is between them.
// ThenBy was usually intended. Checks every query in the expression, including those inside lambdas.
class DiscardedOrderByDetector :
    ExpressionVisitor
{
    static DiscardedOrderByDetector instance = new();

    public static void ThrowIfDiscarded(Expression query) =>
        instance.Visit(query);

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (IsOrderBy(node.Method))
        {
            var discarded = FindOrdering(node.Arguments[0]);
            if (discarded != null)
            {
                throw new($"{Describe(discarded)} is discarded, since it is followed by {node.Describe()}. Use {ThenBy(node)} to add a secondary ordering, or remove the first ordering.");
            }
        }

        return base.VisitMethodCall(node);
    }

    // the closest ordering applied to the source, unless it has been limited since
    static MethodCallExpression? FindOrdering(Expression source)
    {
        while (source is MethodCallExpression { Method.IsStatic: true, Arguments.Count: > 0 } call)
        {
            if (IsOrdering(call.Method))
            {
                return call;
            }

            if (IsRowLimiting(call.Method))
            {
                return null;
            }

            source = call.Arguments[0];
        }

        return null;
    }

    // the whole ordering, for example OrderBy(_ => _.Name).ThenBy(_ => _.Id)
    static string Describe(MethodCallExpression ordering)
    {
        var calls = new List<MethodCallExpression>
        {
            ordering
        };
        while (ordering.Method.Name is nameof(Queryable.ThenBy) or nameof(Queryable.ThenByDescending) &&
               ordering.Arguments[0] is MethodCallExpression source)
        {
            ordering = source;
            calls.Add(ordering);
        }

        calls.Reverse();
        return string.Join('.', calls.Select(_ => _.Describe()));
    }

    static string ThenBy(MethodCallExpression orderBy)
    {
        var descending = orderBy.Method.Name is nameof(Queryable.OrderByDescending) or "OrderDescending";
        var name = nameof(Queryable.ThenBy);
        if (descending)
        {
            name = nameof(Queryable.ThenByDescending);
        }

        // Order() and OrderDescending() order by the element itself
        if (orderBy.Arguments.Count == 1)
        {
            return $"{name}(_ => _)";
        }

        return $"{name}({orderBy.DescribeArguments()})";
    }

    static bool IsLinq(MethodInfo method) =>
        method.DeclaringType == typeof(Queryable) ||
        method.DeclaringType == typeof(Enumerable);

    static bool IsOrderBy(MethodInfo method) =>
        IsLinq(method) &&
        method.Name is
            nameof(Queryable.OrderBy) or
            nameof(Queryable.OrderByDescending) or
            "Order" or
            "OrderDescending";

    static bool IsOrdering(MethodInfo method) =>
        IsOrderBy(method) ||
        (IsLinq(method) &&
         method.Name is
             nameof(Queryable.ThenBy) or
             nameof(Queryable.ThenByDescending));

    static bool IsRowLimiting(MethodInfo method) =>
        IsLinq(method) &&
        method.Name is
            nameof(Queryable.Take) or
            nameof(Queryable.Skip) or
            nameof(Queryable.TakeWhile) or
            nameof(Queryable.SkipWhile) or
            nameof(Queryable.TakeLast) or
            nameof(Queryable.SkipLast);
}
