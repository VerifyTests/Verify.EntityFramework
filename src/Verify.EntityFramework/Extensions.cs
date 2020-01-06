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

}