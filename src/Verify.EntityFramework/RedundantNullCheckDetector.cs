using BinaryExpression = System.Linq.Expressions.BinaryExpression;
using UnaryExpression = System.Linq.Expressions.UnaryExpression;

// In `_.Owner != null && _.Owner.Name == "owner"` the null check is redundant. EF evaluates a member of a null
// navigation as null, and null compared to a non null constant is false, so the comparison already excludes a
// null navigation. This holds in any context, including negated, since the comparison is false exactly when the
// check is. Only comparisons with a non null constant are matched: with != or a value that can be null, a null
// navigation can match, so the check changes the result.
class RedundantNullCheckDetector(IModel model) :
    ExpressionVisitor
{
    public static void ThrowIfRedundant(Expression query, IModel model) =>
        new RedundantNullCheckDetector(model).Visit(query);

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType == ExpressionType.AndAlso)
        {
            var conditions = new List<Expression>();
            AddConditions(node, conditions);
            foreach (var check in conditions)
            {
                var navigation = CheckedNavigation(check);
                if (navigation == null)
                {
                    continue;
                }

                var comparison = conditions.FirstOrDefault(_ => ComparesMember(_, navigation));
                if (comparison != null)
                {
                    throw new($"The null check `{Describe(check)}` is redundant, since `{Describe(comparison)}` is false when {navigation} is null. Remove the null check.");
                }
            }
        }

        return base.VisitBinary(node);
    }

    // BinaryExpression.ToString wraps the expression in parentheses
    static string Describe(Expression expression)
    {
        var text = expression.ToString();
        if (text.StartsWith('(') &&
            text.EndsWith(')'))
        {
            return text[1..^1];
        }

        return text;
    }

    static void AddConditions(Expression expression, List<Expression> conditions)
    {
        if (expression is BinaryExpression { NodeType: ExpressionType.AndAlso } binary)
        {
            AddConditions(binary.Left, conditions);
            AddConditions(binary.Right, conditions);
            return;
        }

        conditions.Add(expression);
    }

    // the navigation in `navigation != null` or `null != navigation`
    Expression? CheckedNavigation(Expression expression)
    {
        if (expression is not BinaryExpression { NodeType: ExpressionType.NotEqual } binary)
        {
            return null;
        }

        var left = Unconvert(binary.Left);
        var right = Unconvert(binary.Right);
        if (IsNull(right) &&
            IsNavigation(left))
        {
            return left;
        }

        if (IsNull(left) &&
            IsNavigation(right))
        {
            return right;
        }

        return null;
    }

    bool IsNavigation(Expression expression) =>
        expression is MemberExpression &&
        model.FindEntityType(expression.Type) != null;

    // `navigation.Member == constant`, or another comparison that is false when the member is null
    static bool ComparesMember(Expression expression, Expression navigation)
    {
        if (expression is not BinaryExpression
            {
                NodeType: ExpressionType.Equal or
                ExpressionType.GreaterThan or
                ExpressionType.GreaterThanOrEqual or
                ExpressionType.LessThan or
                ExpressionType.LessThanOrEqual
            } binary)
        {
            return false;
        }

        var left = Unconvert(binary.Left);
        var right = Unconvert(binary.Right);
        return (IsNonNullConstant(right) && IsMemberOf(left, navigation)) ||
               (IsNonNullConstant(left) && IsMemberOf(right, navigation));
    }

    // a member of the navigation, at any depth, for example navigation.Address.City
    static bool IsMemberOf(Expression expression, Expression navigation)
    {
        while (expression is MemberExpression { Expression: not null } member)
        {
            expression = Unconvert(member.Expression);
            if (ExpressionEqualityComparer.Instance.Equals(expression, navigation))
            {
                return true;
            }
        }

        return false;
    }

    static bool IsNull(Expression expression) =>
        expression is ConstantExpression { Value: null };

    static bool IsNonNullConstant(Expression expression) =>
        expression is ConstantExpression { Value: not null };

    static Expression Unconvert(Expression expression)
    {
        while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
        {
            expression = unary.Operand;
        }

        return expression;
    }
}
