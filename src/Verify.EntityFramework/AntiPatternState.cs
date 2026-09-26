// What the runtime anti-pattern checks have counted for one context. Scoped, so each context has its own, and
// disposed with the context, which is when unmodified tracking is checked.
class AntiPatternState(IDbContextOptions options, ICurrentDbContext currentContext) :
    IDisposable
{
    public AntiPatternOptions Options { get; } =
        options.FindExtension<AntiPatternOptionsExtension>()?.Options ?? new();

    DbContextId? contextId;
    Dictionary<string, int> queries = [];
    int saves;
    int singleRowSaves;
    int trackedLoads;
    bool savedChanges;

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
            trackedLoads = 0;
            savedChanges = false;
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

    public void CountTrackedLoad() =>
        Interlocked.Increment(ref trackedLoads);

    public void SavedChanges() =>
        savedChanges = true;

    public void Dispose()
    {
        if (!Options.ThrowOnUnmodifiedTracking ||
            trackedLoads == 0 ||
            savedChanges)
        {
            return;
        }

        throw new($"The context loaded {trackedLoads} entities with tracking queries, but never saved a change. Use AsNoTracking for queries whose results are not modified.");
    }
}
