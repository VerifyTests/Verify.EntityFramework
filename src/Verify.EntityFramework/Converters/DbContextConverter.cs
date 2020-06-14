using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;
using VerifyTests;

class DbContextConverter :
    WriteOnlyJsonConverter<DbContext>
{
    public override void WriteJson(JsonWriter writer, DbContext? data, JsonSerializer serializer)
    {
        if (data == null)
        {
            return;
        }

        writer.WriteStartObject();

        var entries = data.ChangeTracker.Entries().ToList();
        HandleAdded(entries, writer, serializer);
        HandleModified(entries, writer, serializer);
        HandleDeleted(entries, writer, serializer);

        writer.WriteEndObject();
    }

    static void HandleDeleted(List<EntityEntry> entries,JsonWriter writer, JsonSerializer serializer)
    {
        var deleted = entries
            .Where(x => x.State == EntityState.Deleted)
            .ToList();
        if (!deleted.Any())
        {
            return;
        }
        writer.WritePropertyName("Deleted");
        writer.WriteStartObject();
        foreach (var entry in deleted)
        {
            writer.WritePropertyName(entry.Metadata.DisplayName());
            writer.WriteStartObject();
            WriteId(writer, serializer, entry);
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }

    static void HandleAdded(List<EntityEntry> entries, JsonWriter writer, JsonSerializer serializer)
    {
        var added = entries
            .Where(x => x.State == EntityState.Added)
            .ToList();
        if (!added.Any())
        {
            return;
        }

        writer.WritePropertyName("Added");
        writer.WriteStartObject();
        foreach (var entry in added)
        {
            writer.WritePropertyName(entry.Metadata.DisplayName());
            writer.WriteStartObject();

            foreach (var property in entry.Properties)
            {
                writer.WritePropertyName(property.Metadata.Name);
                serializer.Serialize(writer, property.OriginalValue);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    static void HandleModified(List<EntityEntry> entries,JsonWriter writer, JsonSerializer serializer)
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

        WriteId(writer, serializer, entry);
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

    static void WriteId(JsonWriter writer, JsonSerializer serializer, EntityEntry entry)
    {
        var ids = entry.FindPrimaryKeyValues().ToList();
        if (!ids.Any())
        {
            return;
        }

        if (ids.Count == 1)
        {
            var (name, value) = ids.Single();
            writer.WritePropertyName(name);
            serializer.Serialize(writer, value);
        }
        else
        {
            writer.WritePropertyName("Ids");
            serializer.Serialize(writer, ids);
        }
    }
}