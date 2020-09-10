using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VerifyTests.EntityFramework;

class LogCommandInterceptor :
    DbCommandInterceptor
{
    static AsyncLocal<State?> asyncLocal = new AsyncLocal<State?>();

    public static void Start()
    {
        asyncLocal.Value = new State();
    }

    public static IEnumerable<LogEntry>? Stop()
    {
        var state = asyncLocal.Value;
        asyncLocal.Value = null;
        return state?.Events.OrderBy(x => x.StartTime);
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData data)
    {
        Add("CommandFailed", command, data, data.Exception);
    }

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData data, CancellationToken cancellation)
    {
        Add("CommandFailedAsync", command, data, data.Exception);
        return Task.CompletedTask;
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData data, DbDataReader result)
    {
        Add("ReaderExecuted", command, data);
        return result;
    }

    public override object ScalarExecuted(DbCommand command, CommandExecutedEventData data, object result)
    {
        Add("ScalarExecuted", command, data);
        return result;
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData data, int result)
    {
        Add("NonQueryExecuted", command, data);
        return result;
    }

    public override Task<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData data, DbDataReader result, CancellationToken cancellation)
    {
        Add("ReaderExecutedAsync", command, data);
        return Task.FromResult(result);
    }

    public override Task<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData data, object result, CancellationToken cancellation)
    {
        Add("ScalarExecutedAsync", command, data);
        return Task.FromResult(result);
    }

    public override Task<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData data, int result, CancellationToken cancellation)
    {
        Add("NonQueryExecutedAsync", command, data);
        return Task.FromResult(result);
    }

    void Add(string type, DbCommand command, CommandEndEventData data, Exception? exception = null)
    {
        asyncLocal.Value?.WriteLine(new LogEntry( type,command, data,exception));
    }

    class State
    {
        internal ConcurrentBag<LogEntry> Events = new ConcurrentBag<LogEntry>();

        public void WriteLine(LogEntry entry)
        {
            Events.Add(entry);
        }
    }
}