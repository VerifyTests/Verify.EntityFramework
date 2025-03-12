class LogEntryConverter : WriteOnlyJsonConverter<LogEntry>
{
    public override void Write(VerifyJsonWriter writer, LogEntry logEntry)
    {
        writer.WriteStartObject();

        writer.WriteMember(logEntry, logEntry.Type, "Type");
        writer.WriteMember(logEntry, logEntry.HasTransaction, "HasTransaction");
        writer.WriteMember(logEntry, logEntry.Exception, "Exception");
        writer.WriteMember(logEntry, logEntry.Parameters, "Parameters");
        if (logEntry.IsSqlServer)
        {
            var text = SqlFormatter.Format(logEntry.Text);
            writer.WriteMember(logEntry, text.ToString(), "Text");
        }
        else
        {
            writer.WriteMember(logEntry, logEntry.Text, "Text");
        }


        writer.WriteEndObject();
    }
}