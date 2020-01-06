using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;

static class Extensions
{
    public static IEnumerable<PropertyEntry> ChangedProperties(this EntityEntry entry)
    {
        return entry.Properties
            .Where(x => x.IsModified);
    }

    public static IEnumerable<(string name, object value)> FindPrimaryKeyValues(this EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();
        foreach (var property in primaryKey.Properties)
        {
            var name = property.Name;
            var value = entry.Property(name).CurrentValue;
            yield return (name, value);
        }
    }
}