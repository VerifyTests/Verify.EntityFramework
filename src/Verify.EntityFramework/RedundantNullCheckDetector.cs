using BinaryExpression = System.Linq.Expressions.BinaryExpression;
using UnaryExpression = System.Linq.Expressions.UnaryExpression;

// In `_.Owner != null && _.Owner.Name == "owner"` or `_.Age != null && _.Age > 5` the null check is redundant. EF
// evaluates a member of a null navigation as null, and null compared to a non null constant is false, so the
// comparison already excludes null. This holds in any context, including negated, since the comparison is false
// exactly when the check is. Only comparisons with a non null constant are matched: with != or a value that can be
// null, null can match, so the check changes the result.
class RedundantNullCheckDetector :
    ExpressionVisitor
{
    static RedundantNullCheckDetector instance = new();

    public static void ThrowIfRedundant(Expression query) =>
        instance.Visit(query);

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType == ExpressionType.AndAlso)
        {
            var conditions = new List<Expression>();
            AddConditions(node, conditions);
            foreach (var check in conditions)
            {
                var value = CheckedValue(check);
                if (value == null)
                {
                    continue;
                }

                var comparison = conditions.FirstOrDefault(_ => Compares(_, value));
                if (comparison == null)
                {
                    continue;
                }

                throw new(
                    $"""
                     The null check `{Describe(check)}` is redundant, since `{Describe(comparison)}` is false when {value} is null.
                     Remove the null check.
                     """);
            }
        }

        return base.VisitBinary(node);
    }

    // without the parentheses and conversions that BinaryExpression.ToString adds
    static string Describe(Expression expression)
    {
        if (expression is not BinaryExpression binary)
        {
            return expression.ToString();
        }

        var operation = binary.NodeType switch
        {
            ExpressionType.Equal => "==",
            ExpressionType.NotEqual => "!=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            _ => "<="
        };
        return $"{Describe(Unconvert(binary.Left))} {operation} {Describe(Unconvert(binary.Right))}";
    }

    static void AddConditions(Expression expression, List<Expression> conditions)
    {
        while (true)
        {
            if (expression is BinaryExpression {NodeType: ExpressionType.AndAlso} binary)
            {
                AddConditions(binary.Left, conditions);
                expression = binary.Right;
                continue;
            }

            conditions.Add(expression);
            break;
        }
    }

    // the value in `value != null`, `null != value`, or `value.HasValue`
    static Expression? CheckedValue(Expression expression)
    {
        if (expression is MemberExpression
            {
                Member.Name: "HasValue",
                Expression: { } nullable
            } &&
            IsNullable(nullable.Type))
        {
            return CheckableValue(nullable);
        }

        if (expression is not BinaryExpression
            {
                NodeType: ExpressionType.NotEqual
            } binary)
        {
            return null;
        }

        var left = Unconvert(binary.Left);
        var right = Unconvert(binary.Right);
        if (IsNull(right))
        {
            return CheckableValue(left);
        }

        if (IsNull(left))
        {
            return CheckableValue(right);
        }

        return null;
    }

    // a member that can be null, for example a navigation, a string, or an int?
    static Expression? CheckableValue(Expression expression)
    {
        if (expression is MemberExpression &&
            (!expression.Type.IsValueType || IsNullable(expression.Type)))
        {
            return expression;
        }

        return null;
    }

    static bool IsNullable(Type type) =>
        Nullable.GetUnderlyingType(type) != null;

    // `value == constant`, `value.Member == constant`, or another comparison that is false when value is null
    static bool Compares(Expression expression, Expression value)
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
        return (IsNonNullConstant(right) && IsValueOrMember(left, value)) ||
               (IsNonNullConstant(left) && IsValueOrMember(right, value));
    }

    static bool IsValueOrMember(Expression expression, Expression value) =>
        ExpressionEqualityComparer.Instance.Equals(expression, value) ||
        IsMemberOf(expression, value);

    // a member of the value, at any depth, for example _.Owner.Address.City, or _.Age.Value
    static bool IsMemberOf(Expression expression, Expression value)
    {
        while (expression is MemberExpression { Expression: not null } member)
        {
            expression = Unconvert(member.Expression);
            if (ExpressionEqualityComparer.Instance.Equals(expression, value))
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
        while (expression is UnaryExpression
               {
                   NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked
               } unary)
        {
            expression = unary.Operand;
        }

        return expression;
    }
}
