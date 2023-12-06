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
        if (!TryExecuteQueryable(queryable, out var result))
        {
            writer.Serialize(sql);
            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName("Sql");
        writer.Serialize(sql);
        writer.WritePropertyName("Result");
        writer.Serialize(result);
        writer.WriteEndObject();
    }

    public static bool TryExecuteQueryable(IQueryable queryable, [NotNullWhen(true)] out IList? result)
    {
        var entityType = queryable
            .GetType()
            .GenericTypeArguments.First();

        var executeQueryable = executeQueryableDefinition.MakeGenericMethod(entityType);
        var parameters = new object?[]
        {
            queryable
        };
        try
        {
            result = (IList) executeQueryable.Invoke(null, parameters)!;
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
        if (!type.IsGenericType)
        {
            return false;
        }

        var genericType = type.GetGenericTypeDefinition();
        return genericType == typeof(EntityQueryable<>) ||
               genericType == typeof(IQueryable<>);
    }
}