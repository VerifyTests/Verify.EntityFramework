using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Verify;

class QueryableConverter :
    WriteOnlyJsonConverter
{
    public override void WriteJson(JsonWriter writer, object? context, JsonSerializer serializer)
    {
        if (context == null)
        {
            return;
        }

        var sql = QueryToSql(context);
        serializer.Serialize(writer, sql);
    }

    public static string QueryToSql(object context)
    {
        var entityType = context.GetType().GetGenericArguments().Single();
        var queryableSerializer = typeof(QueryableSerializer<>).MakeGenericType(entityType);
        return (string) queryableSerializer.InvokeMember(
            name: "ToSql",
            invokeAttr: BindingFlags.InvokeMethod,
            binder: null,
            target: null,
            args: new[] {context});
    }

    public override bool CanConvert(Type type)
    {
        return IsQueryable(type);
    }

    public static bool IsQueryable(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        var genericType = type.GetGenericTypeDefinition();
        return genericType == typeof(IQueryable<>);
    }
}