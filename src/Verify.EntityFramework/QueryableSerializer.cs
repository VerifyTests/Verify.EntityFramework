using System.Linq;
using Microsoft.EntityFrameworkCore;

static class QueryableSerializer<TEntity>
    where TEntity : class
{
    public static string ToSql(IQueryable<TEntity> query)
    {
        return query.ToQueryString();
    }
}