using BinaryExpression = System.Linq.Expressions.BinaryExpression;
using UnaryExpression = System.Linq.Expressions.UnaryExpression;

// `_.Employees.Count() > 0` counts every matching row, when only whether one exists is needed. Any stops at the first,
// which EF translates to EXISTS. Only visible inside a query: `query.Count() > 0` compares in C#, after the query.
class CountComparisonDetector :
    ExpressionVisitor
{
    static CountComparisonDetector instance = new();

    public static void ThrowIfCountCompared(Expression query) =>
        instance.Visit(query);

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Unconvert(node.Left);
        var right = Unconvert(node.Right);
        bool? exists = null;
        Expression? count = null;
        if (IsCount(left) &&
            Constant(right) is { } rightValue)
        {
            count = left;
            exists = Exists(node.NodeType, rightValue);
        }
        else if (IsCount(right) &&
                 Constant(left) is { } leftValue)
        {
            count = right;
            exists = Exists(Mirror(node.NodeType), leftValue);
        }

        if (count != null &&
            exists != null)
        {
            var any = Any(count);
            if (exists == false)
            {
                any = $"!{any}";
            }

            throw new(
                $"""
                 `{Describe(node, left, right)}` counts every row, when only whether one exists is needed.
                 Use `{any}`, which stops at the first.
                 """);
        }

        return base.VisitBinary(node);
    }

    // true for `count > 0`, false for `count == 0`, and null for a comparison that needs the count
    static bool? Exists(ExpressionType type, long value) =>
        (type, value) switch
        {
            (ExpressionType.GreaterThan, 0) or
                (ExpressionType.NotEqual, 0) or
                (ExpressionType.GreaterThanOrEqual, 1) => true,
            (ExpressionType.Equal, 0) or
                (ExpressionType.LessThan, 1) or
                (ExpressionType.LessThanOrEqual, 0) => false,
            _ => null
        };

    // `0 < count` is `count > 0`
    static ExpressionType Mirror(ExpressionType type) =>
        type switch
        {
            ExpressionType.GreaterThan => ExpressionType.LessThan,
            ExpressionType.LessThan => ExpressionType.GreaterThan,
            ExpressionType.GreaterThanOrEqual => ExpressionType.LessThanOrEqual,
            ExpressionType.LessThanOrEqual => ExpressionType.GreaterThanOrEqual,
            _ => type
        };

    // Count() or LongCount() from LINQ, or the Count property of a collection
    static bool IsCount(Expression expression)
    {
        if (expression is MethodCallExpression { Method.IsStatic: true, Arguments.Count: > 0 } call)
        {
            var type = call.Method.DeclaringType;
            return (type == typeof(Queryable) || type == typeof(Enumerable)) &&
                   call.Method.Name is nameof(Queryable.Count) or nameof(Queryable.LongCount);
        }

        return expression is MemberExpression { Member.Name: "Count", Expression: { } collection } &&
               collection.Type != typeof(string) &&
               typeof(IEnumerable).IsAssignableFrom(collection.Type);
    }

    static long? Constant(Expression expression)
    {
        if (expression is ConstantExpression { Value: int or long } constant)
        {
            return Convert.ToInt64(constant.Value);
        }

        return null;
    }

    static string Any(Expression count)
    {
        if (count is MemberExpression member)
        {
            return $"{member.Expression}.Any()";
        }

        var call = (MethodCallExpression) count;
        return $"{call.Arguments[0]}.Any({call.DescribeArguments()})";
    }

    static string Describe(BinaryExpression node, Expression left, Expression right)
    {
        var operation = node.NodeType switch
        {
            ExpressionType.Equal => "==",
            ExpressionType.NotEqual => "!=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            _ => node.NodeType.ToString()
        };
        return $"{left} {operation} {right}";
    }

    static Expression Unconvert(Expression expression)
    {
        while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
        {
            expression = unary.Operand;
        }

        return expression;
    }
}
