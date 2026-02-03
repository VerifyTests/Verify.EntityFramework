class ExpressionEntryConverter : WriteOnlyJsonConverter<ExpressionEntry>
{
    public override void Write(VerifyJsonWriter writer, ExpressionEntry entry)
    {
        writer.WriteStartObject();

        writer.WriteMember(entry, entry.Type, "Type");
        writer.WriteMember(entry, entry.Expression, "Expression");

        writer.WriteEndObject();
    }
}
