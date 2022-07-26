using Microsoft.EntityFrameworkCore;

class DbUpdateExceptionConverter :
    WriteOnlyJsonConverter<DbUpdateException>
{
    public override void Write(VerifyJsonWriter writer, DbUpdateException exception)
    {
        writer.WriteStartObject();

        writer.WriteMember(exception, exception.Message, "Message");
        writer.WriteMember(exception, exception.GetType(), "Type");
        writer.WriteMember(exception, exception.InnerException, "InnerException");

        var entriesValue = exception.Entries
            .Select(
                e => new
                {
                    EntryProperties = e.Properties.ToDictionary(
                        p => p.Metadata.Name,
                        p => new
                        {
                            p.OriginalValue,
                            p.CurrentValue,
                            p.IsTemporary,
                            p.IsModified,
                        }),
                    e.State,
                })
            .ToList();

        writer.WriteMember(exception, entriesValue, "Entries");
        writer.WriteMember(exception, exception.StackTrace, "StackTrace");

        writer.WriteEndObject();
    }
}