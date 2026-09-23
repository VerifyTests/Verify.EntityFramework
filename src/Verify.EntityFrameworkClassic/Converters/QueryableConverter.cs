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
        var elementType = FindElementType(data.GetType())!;
        var queryableSerializer = typeof(QueryableSerializer<>).MakeGenericType(elementType);
        return (string) queryableSerializer.InvokeMember(
            name: "ToSql",
            invokeAttr: BindingFlags.InvokeMethod,
            binder: null,
            target: null,
            args: [data])!;
    }

    public override bool CanConvert(Type type) =>
        FindElementType(type) != null;

    // Only EF queries can be converted to sql, so any other IQueryable, for example from AsQueryable(), is left
    // to be serialized as a collection
    public static bool IsQueryable(object target) =>
        FindElementType(target.GetType()) != null;

    // DbSet<> derives from DbQuery<>
    static Type? FindElementType(Type type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (current.IsGenericType &&
                current.GetGenericTypeDefinition() == typeof(DbQuery<>))
            {
                return current.GenericTypeArguments[0];
            }
        }

        return null;
    }
}
