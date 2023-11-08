class LogCommandInterceptor(string? identifier) :
    DbCommandInterceptor
{
    public override void CommandFailed(DbCommand command, CommandErrorEventData data)
        => Add("CommandFailed", command, data, data.Exception);

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData data, Cancel cancel = default)
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

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData data, DbDataReader result, Cancel cancel = default)
    {
        Add("ReaderExecutedAsync", command, data);
        return new(result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData data, object? result, Cancel cancel = default)
    {
        Add("ScalarExecutedAsync", command, data);
        return new(result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData data, int result, Cancel cancel = default)
    {
        Add("NonQueryExecutedAsync", command, data);
        return new(result);
    }

    void Add(string type, DbCommand command, CommandEndEventData data, Exception? exception = null)
    {
        if (identifier is null)
        {
            Recording.Add("sql", new LogEntry(type, command, data, exception));
        }
        else
        {
            Recording.Add(identifier, "sql", new LogEntry(type, command, data, exception));
        }
    }
}