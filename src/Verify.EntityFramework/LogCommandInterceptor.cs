using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

class LogCommandInterceptor :
    DbCommandInterceptor
{
    AsyncLocal<State?> asyncLocal = new AsyncLocal<State?>();

    public void Start()
    {
        asyncLocal.Value = new State();
    }

    public IEnumerable<CommandEndEventData> Stop()
    {
        var state = asyncLocal.Value;
        asyncLocal.Value = null;
        return state!.Datas;
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData data)
    {
        asyncLocal.Value?.WriteLine(data);
    }

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData data, CancellationToken cancellation)
    {
        asyncLocal.Value?.WriteLine(data);
        return Task.CompletedTask;
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData data, DbDataReader result)
    {
        asyncLocal.Value?.WriteLine(data);
        return result;
    }

    public override object ScalarExecuted(DbCommand command, CommandExecutedEventData data, object result)
    {
        asyncLocal.Value?.WriteLine(data);
        return result;
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData data, int result)
    {
        asyncLocal.Value?.WriteLine(data);
        return result;
    }

    public override Task<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData data, DbDataReader result, CancellationToken cancellation)
    {
        asyncLocal.Value?.WriteLine(data);
        return Task.FromResult(result);
    }

    public override Task<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData data, object result, CancellationToken cancellation)
    {
        asyncLocal.Value?.WriteLine(data);
        return Task.FromResult(result);
    }

    public override Task<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData data, int result, CancellationToken cancellation)
    {
        asyncLocal.Value?.WriteLine(data);
        return Task.FromResult(result);
    }

    class State
    {
        internal ConcurrentBag<CommandEndEventData> Datas = new ConcurrentBag<CommandEndEventData>();

        public void WriteLine(CommandEndEventData data)
        {
            Datas.Add(data);
        }
    }
}