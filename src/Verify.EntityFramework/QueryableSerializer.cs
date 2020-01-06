using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

static class QueryableSerializer<TEntity>
    where TEntity : class
{
    public static string ToSql(IQueryable<TEntity> query)
    {
        var enumerator = query.Provider.Execute<IEnumerable<TEntity>>(query.Expression).GetEnumerator();
        var relationalCommandCache = Private(enumerator, "_relationalCommandCache");
        var selectExpression = Private<SelectExpression>(relationalCommandCache, "_selectExpression");
        var factory = Private<IQuerySqlGeneratorFactory>(relationalCommandCache, "_querySqlGeneratorFactory");

        var sqlGenerator = factory.Create();
        var command = sqlGenerator.GetCommand(selectExpression);

        return command.CommandText;
    }

    static object Private(object obj, string privateField)
    {
        return obj.GetType().GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
    }

    static T Private<T>(object obj, string privateField)
    {
        return (T) obj.GetType().GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
    }
}