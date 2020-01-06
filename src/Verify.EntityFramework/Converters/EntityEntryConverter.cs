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

        var entries = context.ChangeTracker.Entries().ToList();
        HandleModified(entries,writer, serializer);

        writer.WriteEndObject();
    }

    void HandleModified(List<EntityEntry> entries,JsonWriter writer, JsonSerializer serializer)
    {
        var modified = entries
            .Where(x => x.State == EntityState.Modified)
            .ToList();
        if (!modified.Any())
        {
            return;
        }
        writer.WritePropertyName("Modified");
        writer.WriteStartObject();
        foreach (var entry in modified)
        {
            HandleModified(writer, serializer,entry);
        }
        writer.WriteEndObject();
    }

    static void HandleModified(JsonWriter writer, JsonSerializer serializer, EntityEntry entry)
    {
        writer.WritePropertyName(entry.Metadata.DisplayName());
        writer.WriteStartObject();
        foreach (var property in entry.ChangedProperties())
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