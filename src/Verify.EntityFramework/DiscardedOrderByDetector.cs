// An OrderBy replaces any earlier ordering, unless a row limiting operator, like Take, is between them.
// ThenBy was usually intended. An operator whose result does not depend on order, like Count, Any, or Single, also
// discards the ordering, and so do GroupBy, ExecuteDelete, and ExecuteUpdate. Checks every query in the expression,
// including those inside lambdas.
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
                throw new(
                    $"""
                     {Describe(discarded)} is discarded, since it is followed by {node.Describe()}.
                     Use {ThenBy(node)} to add a secondary ordering, or remove the first ordering.
                     """);
            }
        }
        else if (IsOrderIndependent(node.Method))
        {
            var discarded = FindOrdering(node.Arguments[0]);
            if (discarded != null)
            {
                throw new(
                    $"""
                     {Describe(discarded)} is discarded, since it is followed by {node.Method.Name}, whose result does not depend on order.
                     Remove the ordering.
                     """);
            }
        }
        else if (IsGroupBy(node.Method))
        {
            var discarded = FindOrdering(node.Arguments[0]);
            if (discarded != null)
            {
                throw new(
                    $"""
                     {Describe(discarded)} is discarded, since it is followed by GroupBy. EF drops an ordering before a GroupBy, so it orders neither the groups nor the elements in them, and First picks by primary key, not by this ordering.
                     Order the elements of each group instead, for example Select(_ => _.OrderBy(...).First()), or order after the GroupBy.
                     """);
            }
        }
        else if (IsBulkOperation(node.Method))
        {
            var discarded = FindOrdering(node.Arguments[0]);
            if (discarded != null)
            {
                throw new(
                    $"""
                     {Describe(discarded)} is discarded, since it is followed by {node.Method.Name}, which changes the same rows whatever the order. EF drops the ordering, but still wraps the rows in a subquery.
                     Remove the ordering.
                     """);
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

    static bool IsOrderIndependent(MethodInfo method) =>
        IsLinq(method) &&
        method.Name is
            nameof(Queryable.Count) or
            nameof(Queryable.LongCount) or
            nameof(Queryable.Any) or
            nameof(Queryable.All) or
            nameof(Queryable.Contains) or
            nameof(Queryable.Sum) or
            nameof(Queryable.Average) or
            nameof(Queryable.Min) or
            nameof(Queryable.Max) or
            // the only element, or an exception, whatever the order
            nameof(Queryable.Single) or
            nameof(Queryable.SingleOrDefault);

    // Unlike LINQ to objects, which keeps the order of the elements in each group
    static bool IsGroupBy(MethodInfo method) =>
        IsLinq(method) &&
        method.Name == nameof(Queryable.GroupBy);

    // ExecuteDeleteAsync and ExecuteUpdateAsync put these in the query too
    static bool IsBulkOperation(MethodInfo method) =>
        method.DeclaringType == typeof(EntityFrameworkQueryableExtensions) &&
        method.Name is
            nameof(EntityFrameworkQueryableExtensions.ExecuteDelete) or
            nameof(EntityFrameworkQueryableExtensions.ExecuteUpdate);

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
