using Microsoft.EntityFrameworkCore;

class DbUpdateExceptionConverter :
    WriteOnlyJsonConverter<DbUpdateException>
{
    public override void Write(VerifyJsonWriter writer, DbUpdateException exception)
    {
        writer.WriteStartObject();

        writer.WriteProperty(exception, exception.GetType(), "Type");
        writer.WriteProperty(exception, exception.InnerException, "InnerException");

        var entriesValue = exception.Entries
            .Select(
                e => new
                {
                    EntryProperties = e.Properties.Select(
                        p => new
                        {
                            PropertyName = p.Metadata.Name,
                            p.OriginalValue,
                            p.CurrentValue,
                            p.IsTemporary,
                            p.IsModified,
                        }),
                    e.State,
                })
            .ToList();

        writer.WriteProperty(exception, entriesValue, "Entries");
        writer.WriteProperty(exception, exception.StackTrace, "StackTrace");

        writer.WriteEndObject();
    }
}