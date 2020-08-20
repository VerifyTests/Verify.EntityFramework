using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;

static class Extensions
{
    public static IEnumerable<PropertyEntry> ChangedProperties(this EntityEntry entry)
    {
        return entry.Properties
            .Where(x =>
            {
                if (!x.IsModified)
                {
                    return false;
                }

                var original = x.OriginalValue;
                var current = x.CurrentValue;
                if (ReferenceEquals(original, current))
                {
                    return false;
                }

                if (original == null)
                {
                    return true;
                }
                return !original.Equals(current);
            });
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

    public static Dictionary<string, object> ToDictionary(this DbParameterCollection collection)
    {
        var dictionary = new Dictionary<string,object>();
        foreach (DbParameter dbParameter in collection)
        {
            dictionary[dbParameter.ParameterName] = dbParameter.Value;
        }

        return dictionary;
    }
}