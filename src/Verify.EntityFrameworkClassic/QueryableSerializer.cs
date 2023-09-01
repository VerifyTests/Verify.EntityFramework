static class QueryableSerializer<TEntity>
    where TEntity : class
{
    public static string ToSql(IQueryable<TEntity> query)
    {
        var linq = GetObjectQuery((DbQuery<TEntity>) query);
        var sql = linq.ToTraceString();
        return linq.Parameters.Aggregate(sql, (current, p) => current.Replace("@" + p.Name, "\'" + p.Value + "\'"));
    }

    static object Private(object obj, string privateField)
        => obj.GetType().GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(obj)!;

    static T Private<T>(object obj, string privateField)
        => (T) obj.GetType().GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(obj)!;

    public static ObjectQuery<T1> GetObjectQuery<T1>(DbQuery<T1> query)
    {
        var internalQuery = Private(query, "_internalQuery");

        return Private<ObjectQuery<T1>>(internalQuery, "_objectQuery");
    }
}