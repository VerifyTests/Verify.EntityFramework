// InMemory executes no DbCommand, so LogCommandInterceptor never sees what it saves. It is recorded here instead,
// where the entries are passed to the database. Unlike a SavingChanges interceptor, this sees changes made by
// interceptors that run later, and cascades that happen during SaveChanges.
class RecordingStateManager(StateManagerDependencies dependencies, IDbContextOptions options) :
    StateManager(dependencies)
{
    LogCommandInterceptor? interceptor = options.FindExtension<RecordingOptionsExtension>()?.Interceptor;
    bool? isInMemory;

    protected override int SaveChanges(IList<IUpdateEntry> entriesToSave)
    {
        Record("SaveChanges", entriesToSave);
        return base.SaveChanges(entriesToSave);
    }

    protected override Task<int> SaveChangesAsync(IList<IUpdateEntry> entriesToSave, Cancel cancel = default)
    {
        Record("SaveChangesAsync", entriesToSave);
        return base.SaveChangesAsync(entriesToSave, cancel);
    }

    void Record(string type, IList<IUpdateEntry> entries)
    {
        if (interceptor != null &&
            (isInMemory ??= Context.IsInMemory()))
        {
            interceptor.AddSaveChanges(type, Context, entries);
        }
    }
}
