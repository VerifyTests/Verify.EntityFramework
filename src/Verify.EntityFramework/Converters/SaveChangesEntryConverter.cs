// Same shape as TrackerConverter
class SaveChangesEntryConverter :
    WriteOnlyJsonConverter<SaveChangesEntry>
{
    public override void Write(VerifyJsonWriter writer, SaveChangesEntry entry)
    {
        writer.WriteStartObject();

        writer.WriteMember(entry, entry.Type, "Type");
        WriteEntities(writer, "Added", entry.Added);
        WriteEntities(writer, "Modified", entry.Modified);
        WriteEntities(writer, "Deleted", entry.Deleted);

        writer.WriteEndObject();
    }

    static void WriteEntities(VerifyJsonWriter writer, string state, List<SavedEntity> entities)
    {
        if (entities.Count == 0)
        {
            return;
        }

        writer.WritePropertyName(state);
        writer.WriteStartObject();
        foreach (var entity in entities)
        {
            writer.WritePropertyName(entity.Name);
            writer.WriteStartObject();
            foreach (var (name, value) in entity.Members)
            {
                writer.WriteMember(entity, value, name);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}
