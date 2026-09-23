// No class constraint, so that a scalar projection, for example Select(_ => _.Id), can be converted
static class QueryableSerializer<TEntity>
{
    public static string ToSql(IQueryable<TEntity> query)
    {
        var linq = GetObjectQuery((DbQuery<TEntity>) query);
        var sql = linq.ToTraceString();
        var parameters = linq.Parameters.ToDictionary(_ => _.Name, _ => _.Value);

        // Match whole parameter names, since replacing @p__linq__1 would also replace the start of @p__linq__10
        return parameterRegex.Replace(
            sql,
            match =>
            {
                if (parameters.TryGetValue(match.Groups[1].Value, out var value))
                {
                    return Inline(value);
                }

                return match.Value;
            });
    }

    static Regex parameterRegex = new(@"@(\w+)");

    // Invariant culture, so that the sql does not depend on the culture of the machine
    static string Inline(object? value)
    {
        var text = Convert.ToString(value, CultureInfo.InvariantCulture)!;
        return $"'{text.Replace("'", "''")}'";
    }

    static object Private(object obj, string privateField)
        => obj
            .GetType()
            .GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(obj)!;

    static T Private<T>(object obj, string privateField)
        => (T) obj
            .GetType()
            .GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(obj)!;

    public static ObjectQuery<T1> GetObjectQuery<T1>(DbQuery<T1> query)
    {
        var internalQuery = Private(query, "_internalQuery");

        return Private<ObjectQuery<T1>>(internalQuery, "_objectQuery");
    }
}