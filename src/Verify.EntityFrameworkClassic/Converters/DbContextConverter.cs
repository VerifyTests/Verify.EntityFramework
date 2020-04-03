using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using Newtonsoft.Json;
using Verify;

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
        HandleModified(entries, writer, serializer, data);
        HandleDeleted(entries, writer, serializer, data);

        writer.WriteEndObject();
    }

    static void HandleDeleted(List<DbEntityEntry> entries, JsonWriter writer, JsonSerializer serializer, DbContext data)
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
            writer.WritePropertyName(entry.Entity.GetType().Name);
            writer.WriteStartObject();
            WriteId(writer, serializer, entry, data);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    static void HandleAdded(List<DbEntityEntry> entries, JsonWriter writer, JsonSerializer serializer)
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
            writer.WritePropertyName(entry.Entity.GetType().Name);
            writer.WriteStartObject();

            foreach (var propertyName in entry.CurrentValues.PropertyNames)
            {
                var value = entry.CurrentValues[propertyName];
                writer.WritePropertyName(propertyName);
                serializer.Serialize(writer, value);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    static void HandleModified(List<DbEntityEntry> entries,JsonWriter writer, JsonSerializer serializer, DbContext context)
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
            HandleModified(writer, serializer, entry, context);
        }

        writer.WriteEndObject();
    }

    static void HandleModified(JsonWriter writer, JsonSerializer serializer, DbEntityEntry entry, DbContext context)
    {
        writer.WritePropertyName(entry.Entity.GetType().Name);
        writer.WriteStartObject();

        WriteId(writer, serializer, entry,context);
        foreach (var property in entry.ChangedProperties())
        {
            writer.WritePropertyName(property.Key);
            serializer.Serialize(
                writer,
                new
                {
                    Original = property.Original,
                    Current = property.Current
                });
        }
        writer.WriteEndObject();
    }

    static void WriteId(JsonWriter writer, JsonSerializer serializer, DbEntityEntry entry, DbContext context)
    {
        var ids = context.FindPrimaryKeyValues(entry).ToList();
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