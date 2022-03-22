using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VerifyTests.EntityFramework;

class LogCommandInterceptor :
    DbCommandInterceptor
{
    static ConcurrentBag<LogEntry>? events = null;

    public static void Start() => events = new();

    public static IEnumerable<LogEntry>? Stop()
    {
        var state = events?.ToArray();
        events = null;
        return state?.OrderBy(x => x.StartTime);
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData data)
        => Add("CommandFailed", command, data, data.Exception);

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData data, CancellationToken cancellation = default)
    {
        Add("CommandFailedAsync", command, data, data.Exception);
        return Task.CompletedTask;
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData data, DbDataReader result)
    {
        Add("ReaderExecuted", command, data);
        return result;
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData data, object? result)
    {
        Add("ScalarExecuted", command, data);
        return result;
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData data, int result)
    {
        Add("NonQueryExecuted", command, data);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData data, DbDataReader result, CancellationToken cancellation = default)
    {
        Add("ReaderExecutedAsync", command, data);
        return new(result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData data, object? result, CancellationToken cancellation = default)
    {
        Add("ScalarExecutedAsync", command, data);
        return new(result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData data, int result, CancellationToken cancellation = default)
    {
        Add("NonQueryExecutedAsync", command, data);
        return new(result);
    }

    static void Add(string type, DbCommand command, CommandEndEventData data, Exception? exception = null)
        => events?.Add(new(type, command, data, exception));
}
