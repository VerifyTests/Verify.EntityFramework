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

    // InMemory executes no DbCommand, so its queries come from RecordingQueryCompiler
    public void AddQuery(string type, DbContext context, Expression query, Dictionary<string, object?> parameters)
    {
        if (!IsRecording() ||
            context.IsRecordingDisabled())
        {
            return;
        }

        var entry = new QueryEntry(
            type,
            ExpressionPrinter.Print(query),
            parameters.ToDictionary(_ => $"@{_.Key}", _ => _.Value));
        Add(entry);
    }

    // InMemory executes no DbCommand, so what it saves comes from RecordingStateManager
    public void AddSaveChanges(string type, DbContext context, IEnumerable<IUpdateEntry> entries)
    {
        if (!IsRecording() ||
            context.IsRecordingDisabled())
        {
            return;
        }

        Add(new SaveChangesEntry(type, entries.Select(_ => _.ToEntityEntry())));
    }

    // Checks IsRecording first, since this runs for every command, and building a LogEntry copies the parameters
    void Add(string type, DbCommand command, CommandEndEventData data, Exception? exception = null)
    {
        if (!IsRecording())
        {
            return;
        }

        var context = data.Context;
        if (context != null &&
            context.IsRecordingDisabled())
        {
            return;
        }

        Add(new LogEntry(type, command, data, exception));
    }

    void Add(object entry)
    {
        if (identifier is null)
        {
            Recording.TryAdd("ef", entry);
        }
        else
        {
            Recording.TryAdd(identifier, "ef", entry);
        }
    }

    bool IsRecording()
    {
        if (identifier is null)
        {
            return Recording.IsRecording();
        }

        return Recording.IsRecording(identifier);
    }
}
