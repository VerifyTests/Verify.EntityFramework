static class Extensions
{
    // same check as EF's IsInMemory(), without a dependency on Microsoft.EntityFrameworkCore.InMemory
    public static bool IsInMemory(this DbContext data) =>
        data.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

    public static IEnumerable<PropertyEntry> ChangedProperties(this EntityEntry entry) =>
        entry.Properties
            .Where(entry =>
            {
                if (!entry.IsModified)
                {
                    return false;
                }

                // EF's comparer, since the original value of a collection is a snapshot copy
                var comparer = entry.Metadata.GetValueComparer();
                return !comparer.Equals(entry.OriginalValue, entry.CurrentValue);
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
            var value = entry.Property(name)
                .CurrentValue;
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