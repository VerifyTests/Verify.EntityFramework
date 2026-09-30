static class Extensions
{
    // same check as EF's IsInMemory(), without a dependency on Microsoft.EntityFrameworkCore.InMemory
    public static bool IsInMemory(this DbContext data) =>
        data.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

    public static IEnumerable<PropertyEntry> ChangedProperties(this EntityEntry entry) =>
        entry.Properties
            .Where(entry =>
            {
                if (!entry.IsModified)
                {
                    return false;
                }

                // EF's comparer, since the original value of a collection is a snapshot copy
                var comparer = entry.Metadata.GetValueComparer();
                return !comparer.Equals(entry.OriginalValue, entry.CurrentValue);
            });

    public static IEnumerable<(string name, object? value)> FindPrimaryKeyValues(this EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();
        if (primaryKey == null)
        {
            yield break;
        }

        foreach (var property in primaryKey.Properties)
        {
            var name = property.Name;
            var value = entry.Property(name)
                .CurrentValue;
            yield return (name, value);
        }
    }

    public static Dictionary<string, object> ToDictionary(this DbParameterCollection collection)
    {
        var dictionary = new Dictionary<string, object>();
        foreach (DbParameter parameter in collection)
        {
            var direction = parameter.Direction;
            if (direction is ParameterDirection.Output or ParameterDirection.ReturnValue)
            {
                continue;
            }

            var nullChar = "";
            if (parameter.IsNullable)
            {
                nullChar = "?";
            }

            var key = $"{parameter.ParameterName} ({parameter.DbType}{nullChar})";
            dictionary[key] = parameter.Value!;
        }

        return dictionary;
    }

    public static Expression Unquote(this Expression expression)
    {
        while (expression is System.Linq.Expressions.UnaryExpression { NodeType: ExpressionType.Quote } quote)
        {
            expression = quote.Operand;
        }

        return expression;
    }

    // for example Include(_ => _.Employees)
    public static string Describe(this MethodCallExpression call) =>
        $"{call.Method.Name}({call.DescribeArguments()})";

    // the arguments after the source
    public static string DescribeArguments(this MethodCallExpression call) =>
        string.Join(
            ", ",
            call.Arguments
                .Skip(1)
                .Select(_ => _.Unquote())
                .Select(_ =>
                {
                    if (_ is ConstantExpression { Value: string value })
                    {
                        return $"\"{value}\"";
                    }

                    // for example the filter keys of IgnoreQueryFilters(["SoftDelete"])
                    if (_ is ConstantExpression { Value: IEnumerable<string> values })
                    {
                        return $"[{string.Join(", ", values.Select(_ => $"\"{_}\""))}]";
                    }

                    return QueryParameterNamer.Instance.Visit(_).ToString();
                }));

    // EF replaces a captured variable with a QueryParameterExpression before the query is intercepted, which
    // ToString describes by its type name. Shows it by its name instead, for example `_ => name`.
    class QueryParameterNamer :
        ExpressionVisitor
    {
        public static QueryParameterNamer Instance { get; } = new();

        protected override Expression VisitExtension(Expression node)
        {
            if (node is QueryParameterExpression parameter)
            {
                return Expression.Parameter(parameter.Type, parameter.Name);
            }

            return base.VisitExtension(node);
        }
    }
}
