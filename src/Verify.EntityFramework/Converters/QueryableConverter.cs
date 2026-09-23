class QueryableConverter :
    WriteOnlyJsonConverter
{
    static MethodInfo executeQueryableDefinition;

    static QueryableConverter() =>
        executeQueryableDefinition = typeof(QueryableConverter).GetMethod("ExecuteQueryable", BindingFlags.NonPublic | BindingFlags.Static)!;

    public override void Write(VerifyJsonWriter writer, object data)
    {
        var queryable = (IQueryable) data;
        var sql = queryable.ToQueryString();
        if (queryable.ShouldFormatSql())
        {
            //TODO: add span support to Serialize
            sql = SqlFormatter.Format(sql).ToString();
        }
        if (!TryExecuteQueryable(queryable, out var result))
        {
            writer.Serialize(sql);
            return;
        }

        writer.WriteStartObject();
        writer.WriteMember(data, sql, "Sql");
        writer.WriteMember(data, result, "Result");
        writer.WriteEndObject();
    }

    public static bool TryExecuteQueryable(IQueryable queryable, [NotNullWhen(true)] out IList? result)
    {
        var executeQueryable = executeQueryableDefinition.MakeGenericMethod(queryable.ElementType);
        try
        {
            result = (IList) executeQueryable.Invoke(null, [queryable])!;
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    static List<T> ExecuteQueryable<T>(IQueryable<T> queryable) =>
        queryable.ToList();

    public override bool CanConvert(Type type)
        => IsQueryable(type);

    public static bool IsQueryable(object target)
    {
        var type = target.GetType();
        return IsQueryable(type);
    }

    static bool IsQueryable(Type type)
    {
        if (type.IsGenericType)
        {
            var genericType = type.GetGenericTypeDefinition();
            if (genericType == typeof(EntityQueryable<>) ||
                genericType == typeof(IQueryable<>))
            {
                return true;
            }
        }

        // Include and ThenInclude return IIncludableQueryable, implemented by a private type
        if (type
            .GetInterfaces()
            .Any(_ => _.IsGenericType &&
                      _.GetGenericTypeDefinition() == typeof(IIncludableQueryable<,>)))
        {
            return true;
        }

        return IsDbSet(type);
    }

    static bool IsDbSet(Type type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (current.IsGenericType &&
                current.GetGenericTypeDefinition() == typeof(DbSet<>))
            {
                return true;
            }
        }

        return false;
    }
}