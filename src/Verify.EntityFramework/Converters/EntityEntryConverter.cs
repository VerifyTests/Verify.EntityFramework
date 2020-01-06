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
            switch (entry.State)
            {
                case EntityState.Detached:
                    break;
                case EntityState.Deleted:
                    break;
                case EntityState.Modified:
                    HandleModified(writer, serializer, entry);
                    break;
                case EntityState.Added:
                    break;
            }
        }

        writer.WriteEndObject();
    }

    static void HandleModified(JsonWriter writer, JsonSerializer serializer, EntityEntry entry)
    {
        var changed = entry.ChangedProperties().ToList();
        if (changed.Any())
        {
            writer.WritePropertyName("Modified");
            writer.WriteStartObject();
            foreach (var property in changed)
            {
                writer.WritePropertyName(property.Metadata.Name);
                serializer.Serialize(
                    writer,
                    new
                    {
                        Original = property.OriginalValue,
                        Current = property.CurrentValue
                    });
            }
            writer.WriteEndObject();
        }
    }
}