﻿class QueryableConverter :
    WriteOnlyJsonConverter
{
    public override void Write(VerifyJsonWriter writer, object data)
    {
        var sql = QueryToSql(data);
        writer.Serialize(sql);
    }

    public static string QueryToSql(object data)
    {
        var queryable = (IQueryable)data;
        return queryable.ToQueryString();
    }

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