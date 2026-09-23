class QueryableConverter :
    WriteOnlyJsonConverter
{
    public override void Write(VerifyJsonWriter writer, object data)
    {
        var sql = QueryToSql(data);
        writer.Serialize(sql);
    }

    public static string QueryToSql(object data)
    {
        var entityType = data
            .GetType()
            .GetGenericArguments()
            .Single();
        var queryableSerializer = typeof(QueryableSerializer<>).MakeGenericType(entityType);
        return (string) queryableSerializer.InvokeMember(
            name: "ToSql",
            invokeAttr: BindingFlags.InvokeMethod,
            binder: null,
            target: null,
            args: [data])!;
    }

    // Was IsQueryable(type), which bound to IsQueryable(object) and tested the Type itself, so was always false
    public override bool CanConvert(Type type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (current.IsGenericType &&
                current.GetGenericTypeDefinition() == typeof(DbQuery<>))
            {
                // QueryableSerializer requires a reference type
                return !current.GenericTypeArguments[0].IsValueType;
            }
        }

        return false;
    }

    public static bool IsQueryable(object target)
        => target is IQueryable;
}