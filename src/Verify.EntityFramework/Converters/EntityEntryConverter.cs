using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;
using Verify;

class DbContextConverter :
    WriteOnlyJsonConverter<DbContext>
{
    public override void WriteJson(JsonWriter writer, DbContext? context, JsonSerializer serializer)
    {
        if (context == null)
        {
            return;
        }

        writer.WriteStartObject();

        foreach (var entry in context.ChangeTracker.Entries()
            .Where(x => x.State != EntityState.Unchanged))
        {
            foreach (var property in entry.ChangedProperties())
            {
                writer.WritePropertyName(property.Metadata.Name);
                serializer.Serialize(
                    writer,
                    new
                    {
                        Original = property.OriginalValue,
                        Current = property.CurrentValue,
                    });
            }
        }

        writer.WriteEndObject();
    }
}