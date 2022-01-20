using Newtonsoft.Json;

class QueryableConverter :
    WriteOnlyJsonConverter
{
    public override void Write(VerifyJsonWriter writer, object data, JsonSerializer serializer)
    {
        var sql = QueryToSql(data);
        serializer.Serialize(writer, sql);
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
    {
        return IsQueryable(type);
    }

    public static bool IsQueryable(object target)
    {
        return target is IQueryable;
    }
}