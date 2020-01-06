using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query.Internal;
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

        var entityType = context.GetType().GetGenericArguments().Single();
        var queryableSerializer = typeof(QueryableSerializer<>).MakeGenericType(entityType);
        var memberInfos = queryableSerializer.GetMembers(BindingFlags.Static|BindingFlags.NonPublic);
        var sql = (string)queryableSerializer.InvokeMember(
            name: "ToSql",
            invokeAttr: BindingFlags.InvokeMethod,
            binder: null,
            target: null,
            args: new []{context});
        serializer.Serialize(writer, sql);
    }

    public override bool CanConvert(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        var genericType = type.GetGenericTypeDefinition();
        return genericType == typeof(EntityQueryable<>);
    }
}