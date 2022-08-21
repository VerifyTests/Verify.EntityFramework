using System.Data.Common;
using Argon;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace VerifyTests.EntityFramework;

public class LogEntry
{
    public LogEntry(string type, DbCommand command, CommandEndEventData data, Exception? exception = null)
    {
        Parameters = command.Parameters.ToDictionary();
        Text = command.CommandText.Trim();
        HasTransaction = command.Transaction != null;
        Type = type;
        Duration = data.Duration;
        Exception = exception;
        StartTime = data.StartTime;
    }

    public string Type { get; }
    [JsonIgnore]
    public DateTimeOffset StartTime { get; }
    [JsonIgnore]
    public TimeSpan Duration { get; }
    public bool HasTransaction { get; }
    public Exception? Exception { get; }
    public IDictionary<string, object> Parameters { get; }
    public string Text { get; }
}