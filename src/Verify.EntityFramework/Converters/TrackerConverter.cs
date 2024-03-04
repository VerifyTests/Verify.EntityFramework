class TrackerConverter :
    WriteOnlyJsonConverter<ChangeTracker>
{
    public override void Write(VerifyJsonWriter writer, ChangeTracker tracker)
    {
        writer.WriteStartObject();

        var entries = tracker
            .Entries()
            .ToList();
        HandleAdded(entries, writer);
        HandleModified(entries, writer);
        HandleDeleted(entries, writer);

        writer.WriteEndObject();
    }

    static void HandleDeleted(List<EntityEntry> entries, VerifyJsonWriter writer)
    {
        var deleted = entries
            .Where(_ => _.State == EntityState.Deleted)
            .ToList();
        if (deleted.Count == 0)
        {
            return;
        }

        writer.WritePropertyName("Deleted");
        writer.WriteStartObject();
        foreach (var entry in deleted)
        {
            writer.WritePropertyName(entry.Metadata.DisplayName());
            writer.WriteStartObject();
            WriteId(writer, entry);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    static void HandleAdded(List<EntityEntry> entries, VerifyJsonWriter writer)
    {
        var added = entries
            .Where(_ => _.State == EntityState.Added)
            .ToList();
        if (added.Count == 0)
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
                writer.WriteMember(entry, property.OriginalValue, property.Metadata.Name);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    static void HandleModified(List<EntityEntry> entries, VerifyJsonWriter writer)
    {
        var modified = entries
            .Where(_ => _.State == EntityState.Modified)
            .ToList();
        if (modified.Count == 0)
        {
            return;
        }

        writer.WritePropertyName("Modified");
        writer.WriteStartObject();
        foreach (var entry in modified)
        {
            HandleModified(writer, entry);
        }

        writer.WriteEndObject();
    }

    static void HandleModified(VerifyJsonWriter writer, EntityEntry entry)
    {
        writer.WritePropertyName(entry.Metadata.DisplayName());
        writer.WriteStartObject();

        WriteId(writer, entry);
        foreach (var property in entry.ChangedProperties())
        {
            writer.WritePropertyName(property.Metadata.Name);
            writer.Serialize(
                new
                {
                    Original = property.OriginalValue,
                    Current = property.CurrentValue
                });
        }

        writer.WriteEndObject();
    }

    static void WriteId(VerifyJsonWriter writer, EntityEntry entry)
    {
        var ids = entry
            .FindPrimaryKeyValues()
            .ToList();
        if (ids.Count == 0)
        {
            return;
        }

        if (ids.Count == 1)
        {
            var (name, value) = ids.Single();
            writer.WriteMember(entry, value, name);
        }
        else
        {
            writer.WriteMember(entry, ids, "Ids");
        }
    }
}