
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
        var entityType = data.GetType().GetGenericArguments().Single();
        var queryableSerializer = typeof(QueryableSerializer<>).MakeGenericType(entityType);
        return (string) queryableSerializer.InvokeMember(
            name: "ToSql",
            invokeAttr: BindingFlags.InvokeMethod,
            binder: null,
            target: null,
            args: new[] {data});
    }

    public override bool CanConvert(Type type)
        => IsQueryable(type);

    public static bool IsQueryable(object target)
        => target is IQueryable;
}