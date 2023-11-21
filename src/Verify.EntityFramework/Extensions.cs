static class Extensions
{
    static MethodInfo setMethod = typeof(DbContext)
        .GetMethod("Set", Array.Empty<Type>())!;

    static MethodInfo asNoTracking = typeof(EntityFrameworkQueryableExtensions).GetMethod("AsNoTracking")!;

    public static IQueryable<object> AsNoTracking(this IQueryable<object> set, Type clrType)
    {
        var genericNoTracking = asNoTracking.MakeGenericMethod(clrType);
        return (IQueryable<object>) genericNoTracking.Invoke(null, new object[] {set})!;
    }

    public static IQueryable<object> Set(this DbContext data, Type t) =>
        (IQueryable<object>) setMethod.MakeGenericMethod(t)
            .Invoke(data, null)!;

    public static IOrderedEnumerable<IEntityType> EntityTypes(this DbContext data) =>
        data.Model
            .GetEntityTypes()
            .OrderBy(_ => _.Name);

    public static IEnumerable<PropertyEntry> ChangedProperties(this EntityEntry entry) =>
        entry.Properties
            .Where(entry =>
            {
                if (!entry.IsModified)
                {
                    return false;
                }

                var original = entry.OriginalValue;
                var current = entry.CurrentValue;
                if (ReferenceEquals(original, current))
                {
                    return false;
                }

                if (original is null)
                {
                    return true;
                }

                return !original.Equals(current);
            });

    public static IEnumerable<(string name, object? value)> FindPrimaryKeyValues(this EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();
        if (primaryKey == null)
        {
            yield break;
        }

        foreach (var property in primaryKey.Properties)
        {
            var name = property.Name;
            var value = entry.Property(name).CurrentValue;
            yield return (name, value);
        }
    }

    public static Dictionary<string, object> ToDictionary(this DbParameterCollection collection)
    {
        var dictionary = new Dictionary<string, object>();
        foreach (DbParameter parameter in collection)
        {
            var direction = parameter.Direction;
            if (direction is ParameterDirection.Output or ParameterDirection.ReturnValue)
            {
                continue;
            }

            var nullChar = "";
            if (parameter.IsNullable)
            {
                nullChar = "?";
            }

            var key = $"{parameter.ParameterName} ({parameter.DbType}{nullChar})";
            dictionary[key] = parameter.Value!;
        }

        return dictionary;
    }
}