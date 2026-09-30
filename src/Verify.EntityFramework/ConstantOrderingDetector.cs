// `OrderBy(_ => 1)`, or ordering by a captured variable, is translated to `ORDER BY (SELECT 1)`, so it does not order
// the rows. It also stops EF logging RowLimitingOperationWithoutOrderByWarning for a Take or Skip after it, which makes
// it a tempting way to silence that warning. Checks every query in the expression, including those inside lambdas.
class ConstantOrderingDetector :
    ExpressionVisitor
{
    static ConstantOrderingDetector instance = new();

    public static void ThrowIfConstant(Expression query) =>
        instance.Visit(query);

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (IsOrdering(node.Method) &&
            node.Arguments[1].Unquote() is LambdaExpression selector &&
            !ParameterFinder.Uses(selector.Body, selector.Parameters[0]))
        {
            throw new(
                $"""
                 {node.Describe()} orders by a value that is the same for every row, so it does not order the rows.
                 Order by a member of the row, for example its key, or remove the ordering.
                 """);
        }

        return base.VisitMethodCall(node);
    }

    // Order() and OrderDescending() have no key selector, so order by the element itself
    static bool IsOrdering(MethodInfo method) =>
        (method.DeclaringType == typeof(Queryable) ||
         method.DeclaringType == typeof(Enumerable)) &&
        method.Name is
            nameof(Queryable.OrderBy) or
            nameof(Queryable.OrderByDescending) or
            nameof(Queryable.ThenBy) or
            nameof(Queryable.ThenByDescending);

    class ParameterFinder(ParameterExpression parameter) :
        ExpressionVisitor
    {
        bool found;

        public static bool Uses(Expression expression, ParameterExpression parameter)
        {
            var finder = new ParameterFinder(parameter);
            finder.Visit(expression);
            return finder.found;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == parameter)
            {
                found = true;
            }

            return node;
        }
    }
}
