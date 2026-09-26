using UnaryExpression = System.Linq.Expressions.UnaryExpression;

// `_.Name.ToLower() == "x"` in a filter, ordering, or join wraps the column in a function, so the database can not use an
// index on it. SQL Server compares case insensitively with its default collation, which makes the conversion redundant
// too. A conversion of a variable is fine, since EF sends it as a parameter, and so is one in a projection, since it only
// changes the output.
class CaseConversionDetector :
    ExpressionVisitor
{
    static CaseConversionDetector instance = new();

    public static void ThrowIfColumnConverted(Expression query) =>
        instance.Visit(query);

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (IsFilterOrderOrJoin(node.Method))
        {
            foreach (var argument in node.Arguments.Skip(1))
            {
                if (argument.Unquote() is LambdaExpression lambda &&
                    ColumnConversionFinder.Find(lambda.Body) is { } conversion)
                {
                    throw new($"`{conversion}` in {node.Method.Name} converts the column, so the database can not use an index on it. SQL Server compares case insensitively with its default collation, so compare the column directly. For a case sensitive column, use EF.Functions.Collate with a case insensitive collation.");
                }
            }
        }

        return base.VisitMethodCall(node);
    }

    static bool IsFilterOrderOrJoin(MethodInfo method) =>
        (method.DeclaringType == typeof(Queryable) || method.DeclaringType == typeof(Enumerable)) &&
        method.Name is
            nameof(Queryable.Where) or
            nameof(Queryable.OrderBy) or
            nameof(Queryable.OrderByDescending) or
            nameof(Queryable.ThenBy) or
            nameof(Queryable.ThenByDescending) or
            nameof(Queryable.Join) or
            nameof(Queryable.GroupJoin) or
            nameof(Queryable.Any) or
            nameof(Queryable.All) or
            nameof(Queryable.Count) or
            nameof(Queryable.LongCount) or
            nameof(Queryable.First) or
            nameof(Queryable.FirstOrDefault) or
            nameof(Queryable.Single) or
            nameof(Queryable.SingleOrDefault) or
            nameof(Queryable.Last) or
            nameof(Queryable.LastOrDefault) or
            nameof(Queryable.TakeWhile) or
            nameof(Queryable.SkipWhile);

    // finds ToLower, ToUpper, ToLowerInvariant, or ToUpperInvariant called on a column
    class ColumnConversionFinder :
        ExpressionVisitor
    {
        MethodCallExpression? found;

        public static MethodCallExpression? Find(Expression expression)
        {
            var finder = new ColumnConversionFinder();
            finder.Visit(expression);
            return finder.found;
        }

        public override Expression? Visit(Expression? node)
        {
            if (found != null)
            {
                return node;
            }

            return base.Visit(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(string) &&
                node.Method.Name is
                    nameof(string.ToLower) or
                    nameof(string.ToUpper) or
                    nameof(string.ToLowerInvariant) or
                    nameof(string.ToUpperInvariant) &&
                node.Object != null &&
                IsColumn(node.Object))
            {
                found = node;
                return node;
            }

            return base.VisitMethodCall(node);
        }

        // a member reached from a lambda parameter, for example _.Name or _.Company.Name, or EF.Property(_, "Name")
        static bool IsColumn(Expression expression)
        {
            var current = expression;
            while (true)
            {
                switch (current)
                {
                    case MemberExpression { Expression: { } inner }:
                        current = inner;
                        continue;
                    case MethodCallExpression { Method.Name: nameof(EF.Property) } call when call.Method.DeclaringType == typeof(EF):
                        current = call.Arguments[0];
                        continue;
                    case UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.TypeAs } unary:
                        current = unary.Operand;
                        continue;
                    case ParameterExpression:
                        return current != expression;
                    default:
                        return false;
                }
            }
        }
    }
}
