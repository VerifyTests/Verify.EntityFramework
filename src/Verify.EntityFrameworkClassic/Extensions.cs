using System.Data.Entity;
using System.Data.Entity.Infrastructure;

static class Extensions
{

    public static IEnumerable<ChangePair> ChangedProperties(this DbEntityEntry entry)
    {
        foreach (var name in entry.CurrentValues.PropertyNames)
        {
            var current = entry.CurrentValues[name];
            var original = entry.OriginalValues[name];
            if (ReferenceEquals(original, current))
            {
                continue;
            }

            if (original is not null)
            {
                if (original.Equals(current))
                {
                    continue;
                }
            }

            yield return new(name, original, current);
        }
    }

    public static IEnumerable<(string name, object value)> FindPrimaryKeyValues(this DbContext context, DbEntityEntry entry)
    {
        var objectContext = ((IObjectContextAdapter)context).ObjectContext;

        var setBase = objectContext.ObjectStateManager
            .GetObjectStateEntry(entry.Entity).EntitySet;

        foreach (var property in setBase.ElementType.KeyMembers)
        {
            var name = property.Name;
            var value = entry.Property(name).CurrentValue;
            yield return (name, value);
        }
    }
}