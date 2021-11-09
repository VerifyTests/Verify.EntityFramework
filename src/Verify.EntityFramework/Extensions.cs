using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

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

    public static IQueryable<object> Set(this DbContext data, Type t)
    {
        return (IQueryable<object>) setMethod.MakeGenericMethod(t)
            .Invoke(data, null)!;
    }

    public static IOrderedEnumerable<IEntityType> EntityTypes(this DbContext data)
    {
        return data.Model
            .GetEntityTypes()
            .OrderBy(x => x.Name);
    }

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

                if (original is null)
                {
                    return true;
                }

                return !original.Equals(current);
            });
    }

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
        Dictionary<string, object> dictionary = new();
        foreach (DbParameter parameter in collection)
        {
            var direction = parameter.Direction;
            if (direction == ParameterDirection.Output ||
                direction == ParameterDirection.ReturnValue)
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