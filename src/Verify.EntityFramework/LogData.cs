﻿namespace VerifyTests.EntityFramework;

public class LogEntry(
    string type,
    DbCommand command,
    CommandEndEventData data,
    Exception? exception = null)
{
    public string Type { get; } = type;
    public DateTimeOffset StartTime { get; } = data.StartTime;
    internal bool IsSqlServer { get; } = command.GetType().Name == "SqlCommand";
    public TimeSpan Duration { get; } = data.Duration;
    public bool HasTransaction { get; } = command.Transaction != null;
    public Exception? Exception { get; } = exception;
    public IDictionary<string, object> Parameters { get; } = command.Parameters.ToDictionary();
    public string Text { get; } = command.CommandText.Trim();
}