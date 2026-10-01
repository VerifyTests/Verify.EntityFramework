// A query that EF builds itself, which the code under test can not change, so is not checked for anti-patterns.
// EntityEntry.Reload and GetDatabaseValues query the row of an entry by its key, as
// root.AsNoTracking().IgnoreQueryFilters().Where(key).Select(_ => object[]), then FirstOrDefault.
static class InternalQuery
{
    public static bool Is(Expression query)
    {
        var expression = query;
        while (expression is MethodCallExpression { Method.IsStatic: true, Arguments.Count: > 0 } call)
        {
            if (IsDatabaseValues(call))
            {
                return true;
            }

            expression = call.Arguments[0];
        }

        return false;
    }

    static bool IsDatabaseValues(MethodCallExpression select)
    {
        if (!Is(select, typeof(Queryable), nameof(Queryable.Select)) ||
            select.Type != typeof(IQueryable<object[]>) ||
            select.Arguments[0] is not MethodCallExpression where ||
            !Is(where, typeof(Queryable), nameof(Queryable.Where)) ||
            where.Arguments[0] is not MethodCallExpression ignore ||
            !Is(ignore, typeof(EntityFrameworkQueryableExtensions), nameof(EntityFrameworkQueryableExtensions.IgnoreQueryFilters)) ||
            ignore.Arguments[0] is not MethodCallExpression noTracking ||
            !Is(noTracking, typeof(EntityFrameworkQueryableExtensions), nameof(EntityFrameworkQueryableExtensions.AsNoTracking)))
        {
            return false;
        }

        return noTracking.Arguments[0] is EntityQueryRootExpression;
    }

    static bool Is(MethodCallExpression call, Type type, string name) =>
        call.Method.DeclaringType == type &&
        call.Method.Name == name;
}
