// What the runtime anti-pattern checks have counted for one context. Scoped, so each context has its own.
class AntiPatternState(IDbContextOptions options, ICurrentDbContext currentContext)
{
    public AntiPatternOptions Options { get; } =
        options.FindExtension<AntiPatternOptionsExtension>()?.Options ?? new();

    // the recording that EnableRecording set up, which may have an identifier
    LogCommandInterceptor? recording = options.FindExtension<RecordingOptionsExtension>()?.Interceptor;

    DbContextId? contextId;
    Dictionary<string, int> queries = [];
    int saves;
    int singleRowSaves;

    // Whether the checks apply now. By default only while Verify is recording, which is the code under test, so the
    // setup and assertions of a test are not counted.
    public bool IsActive
    {
        get
        {
            if (!Options.OnlyWhileRecording)
            {
                return true;
            }

            if (recording != null)
            {
                return recording.IsRecording();
            }

            return Recording.IsRecording();
        }
    }

    // A pooled context keeps this state across leases, so the counts start again for each lease
    public AntiPatternState ForLease()
    {
        var id = currentContext.Context.ContextId;
        if (contextId != id)
        {
            contextId = id;
            queries.Clear();
            saves = 0;
            singleRowSaves = 0;
        }

        return this;
    }

    public int CountQuery(string sql)
    {
        lock (queries)
        {
            queries.TryGetValue(sql, out var count);
            count++;
            queries[sql] = count;
            return count;
        }
    }

    public int CountSave() =>
        Interlocked.Increment(ref saves);

    public int CountSingleRowSave() =>
        Interlocked.Increment(ref singleRowSaves);
}
