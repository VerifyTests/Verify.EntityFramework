class LogCommandInterceptor :
    DbCommandInterceptor
{
    static AsyncLocal<State?> asyncLocal = new();
    static ConcurrentDictionary<string, List<LogEntry>> namedEvents = new(StringComparer.OrdinalIgnoreCase);
    string? identifier;

    public static void Start() => asyncLocal.Value = new();
    public static void Start(string identifier) => namedEvents.GetOrAdd(identifier, _ => new());

    public static IReadOnlyList<LogEntry>? Stop()
    {
        var state = asyncLocal.Value;
        asyncLocal.Value = null;
        return state?.Events;
    }

    public static IReadOnlyList<LogEntry>? Stop(string identifier)
    {
        namedEvents.TryRemove(identifier, out var state);

        return state;
    }

    public LogCommandInterceptor(string? identifier) =>
        this.identifier = identifier;

    public override void CommandFailed(DbCommand command, CommandErrorEventData data)
        => Add("CommandFailed", command, data, data.Exception);

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData data, Cancellation cancellation = default)
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

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData data, DbDataReader result, Cancellation cancellation = default)
    {
        Add("ReaderExecutedAsync", command, data);
        return new(result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData data, object? result, Cancellation cancellation = default)
    {
        Add("ScalarExecutedAsync", command, data);
        return new(result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData data, int result, Cancellation cancellation = default)
    {
        Add("NonQueryExecutedAsync", command, data);
        return new(result);
    }

    void Add(string type, DbCommand command, CommandEndEventData data, Exception? exception = null)
    {
        if (identifier is null)
        {
            asyncLocal.Value?.WriteLine(new(type, command, data, exception));
        }
        else if (namedEvents.ContainsKey(identifier))
        {
            namedEvents[identifier].Add(new(type, command, data, exception));
        }
    }

    class State
    {
        internal List<LogEntry> Events = new();

        public void WriteLine(LogEntry entry)
            => Events.Add(entry);
    }
}