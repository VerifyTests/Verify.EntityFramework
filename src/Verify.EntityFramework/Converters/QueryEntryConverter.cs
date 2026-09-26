class QueryEntryConverter :
    WriteOnlyJsonConverter<QueryEntry>
{
    public override void Write(VerifyJsonWriter writer, QueryEntry entry)
    {
        writer.WriteStartObject();

        writer.WriteMember(entry, entry.Type, "Type");
        writer.WriteMember(entry, entry.Parameters, "Parameters");
        writer.WriteMember(entry, entry.Text, "Text");

        writer.WriteEndObject();
    }
}
