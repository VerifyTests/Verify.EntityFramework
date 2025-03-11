class DbUpdateExceptionConverter :
    WriteOnlyJsonConverter<DbUpdateException>
{
    public override void Write(VerifyJsonWriter writer, DbUpdateException exception)
    {
        writer.WriteStartObject();

        writer.WriteMember(exception, exception.Message, "Message");
        writer.WriteMember(exception, exception.GetType(), "Type");
        writer.WriteMember(exception, exception.InnerException, "InnerException");

        var entries = exception
            .Entries
            .Select(entry => new
            {
                EntryProperties = entry.Properties
                    .ToDictionary(
                        _ => _.Metadata.Name,
                        _ => new
                        {
                            _.OriginalValue,
                            _.CurrentValue,
                            _.IsTemporary,
                            _.IsModified
                        }),
                entry.State
            });

        writer.WriteMember(exception, entries, "Entries");
        writer.WriteMember(exception, exception.StackTrace, "StackTrace");

        writer.WriteEndObject();
    }
}