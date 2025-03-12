class LogEntryConverter : WriteOnlyJsonConverter<LogEntry>
{
    public override void Write(VerifyJsonWriter writer, LogEntry logEntry)
    {
        writer.WriteStartObject();

        writer.WriteMember(logEntry, logEntry.Type, "Type");
        writer.WriteMember(logEntry, logEntry.HasTransaction, "HasTransaction");
        writer.WriteMember(logEntry, logEntry.Exception, "Exception");
        writer.WriteMember(logEntry, logEntry.Parameters, "Parameters");
        ReadOnlySpan<char> text;
        if (logEntry.IsSqlServer)
        {
            text = SqlFormatter.Format(logEntry.Text);
        }
        else
        {
            text = logEntry.Text.AsSpan();
        }

        writer.WriteMember(logEntry, text, "Text");

        writer.WriteEndObject();
    }
}