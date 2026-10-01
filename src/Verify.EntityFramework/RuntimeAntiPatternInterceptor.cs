// The anti-pattern checks that are only visible while a context runs. Each is opt in, through AntiPatternOptions.
// Stateless: what each context has counted is in its AntiPatternState.
class RuntimeAntiPatternInterceptor :
    DbCommandInterceptor,
    ISaveChangesInterceptor,
    IMaterializationInterceptor
{
    public static RuntimeAntiPatternInterceptor Instance { get; } = new();

    // Records the entities a tracking query loads, for ThrowOnRedundantUpdate. Recorded whether or not Verify is
    // recording, since the setup of a test often loads what the code under test updates.
    public object InitializedInstance(MaterializationInterceptionData data, object entity)
    {
        if (data.QueryTrackingBehavior != QueryTrackingBehavior.TrackAll)
        {
            return entity;
        }

        var state = State(data.Context);
        if (state.Options.ThrowOnRedundantUpdate)
        {
            state.AddLoaded(entity);
        }

        return entity;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData data, InterceptionResult<DbDataReader> result)
    {
        CheckSynchronous(data);
        CheckRepeated(command, data);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData data, InterceptionResult<DbDataReader> result, Cancel cancel = default)
    {
        CheckRepeated(command, data);
        return new(result);
    }

    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData data, InterceptionResult<object> result)
    {
        CheckSynchronous(data);
        CheckRepeated(command, data);
        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData data, InterceptionResult<object> result, Cancel cancel = default)
    {
        CheckRepeated(command, data);
        return new(result);
    }

    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData data, InterceptionResult<int> result)
    {
        CheckSynchronous(data);
        return result;
    }

    public InterceptionResult<int> SavingChanges(DbContextEventData data, InterceptionResult<int> result)
    {
        var context = data.Context;
        if (context == null)
        {
            return result;
        }

        var state = State(context);
        if (!state.IsActive)
        {
            return result;
        }

        if (state.Options.ThrowOnSynchronousCalls)
        {
            throw new(
                """
                SaveChanges executed synchronously.
                Use SaveChangesAsync.
                """);
        }

        CheckSave(context, state);
        return result;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, Cancel cancel = default)
    {
        var context = data.Context;
        if (context == null)
        {
            return new(result);
        }

        var state = State(context);
        if (state.IsActive)
        {
            CheckSave(context, state);
        }

        return new(result);
    }

    static AntiPatternState State(DbContext context) =>
        context.GetService<AntiPatternState>().ForLease();

    static void CheckSynchronous(CommandEventData data)
    {
        var context = data.Context;
        if (context == null ||
            !IsUserCommand(data.CommandSource))
        {
            return;
        }

        var state = State(context);
        if (!state.Options.ThrowOnSynchronousCalls ||
            !state.IsActive)
        {
            return;
        }

        throw new(
            $"""
             A {data.CommandSource} command executed synchronously.
             Use the async method, for example ToListAsync, SaveChangesAsync, or ExecuteDeleteAsync.
             """);
    }

    // commands that EF runs itself, like migrations and value generation, are not the caller's choice
    static bool IsUserCommand(CommandSource source) =>
        source is
            CommandSource.LinqQuery or
            CommandSource.SaveChanges or
            CommandSource.FromSqlQuery or
            CommandSource.ExecuteSqlRaw or
            CommandSource.ExecuteDelete or
            CommandSource.ExecuteUpdate;

    static void CheckRepeated(DbCommand command, CommandEventData data)
    {
        var context = data.Context;
        if (context == null ||
            data.CommandSource
                is not (
                CommandSource.LinqQuery or
                CommandSource.FromSqlQuery))
        {
            return;
        }

        var state = State(context);
        var options = state.Options;
        if (!options.ThrowOnRepeatedQueries ||
            !state.IsActive)
        {
            return;
        }

        var count = state.CountQuery(command.CommandText);
        if (count > options.RepeatedQueryThreshold)
        {
            throw new(
                $"""
                 The same query executed {count} times in one context, which usually means a query in a loop (N+1).
                 Load the data in one query, for example with Include, a projection, or Contains.
                 Query:{Environment.NewLine}{command.CommandText}
                 """);
        }
    }

    static void CheckSave(DbContext context, AntiPatternState state)
    {
        var options = state.Options;
        var entries = context.ChangeTracker
            .Entries()
            .Where(_ => _.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();
        if (entries.Count == 0)
        {
            return;
        }

        if (options.ThrowOnRepeatedSaveChanges)
        {
            var saves = state.CountSave();
            if (saves > options.RepeatedSaveChangesThreshold)
            {
                throw new(
                    $"""
                     SaveChanges saved changes {saves} times in one context, which usually means SaveChanges in a loop.
                     Make all the changes, then call SaveChanges once.
                     """);
            }
        }

        if (options.ThrowOnSingleRowSaves &&
            entries.Count == 1)
        {
            var saves = state.CountSingleRowSave();
            if (saves > options.SingleRowSavesThreshold)
            {
                throw new(
                    $"""
                     SaveChanges saved a single entity {saves} times in one context, which usually means saving one entity per iteration of a loop.
                     Add or change all the entities, then call SaveChanges once.
                     """);
            }
        }

        if (options.ThrowOnLoadThenModify &&
            entries.Count > options.LoadThenModifyThreshold)
        {
            CheckLoadThenModify(entries);
        }

        if (options.ThrowOnRedundantUpdate)
        {
            foreach (var entry in entries)
            {
                CheckRedundantUpdate(entry, state);
            }
        }
    }

    // Update(), or setting State to Modified, marks every property as modified, while change tracking only marks those
    // that changed. So every property marked, when some are unchanged, on an entity a tracking query loaded, means one
    // of those calls. A disconnected entity passed to Update() is not matched, since no query loaded it.
    static void CheckRedundantUpdate(EntityEntry entry, AntiPatternState state)
    {
        if (entry.State != EntityState.Modified ||
            !state.WasLoaded(entry.Entity))
        {
            return;
        }

        var properties = entry.Properties
            .Where(_ => !_.Metadata.IsPrimaryKey() &&
                        _.Metadata.GetAfterSaveBehavior() == PropertySaveBehavior.Save)
            .ToList();
        if (properties.Count == 0 ||
            properties.Any(_ => !_.IsModified))
        {
            return;
        }

        var changed = entry
            .ChangedProperties()
            .Select(_ => _.Metadata.Name)
            .ToList();
        if (changed.Count == properties.Count)
        {
            return;
        }

        var name = entry.Metadata.DisplayName();
        var what = "none of them changed";
        if (changed.Count > 0)
        {
            what = $"only {string.Join(", ", changed)} changed";
        }

        throw new(
            $"""
             SaveChanges updates every column of {name}, though {what}. Update(), or setting State to Modified, marks every property as modified, but the context already tracks this {name}, which a query loaded, and detects its changes.
             Remove the Update() call, or the State change, so only the changed columns are written.
             """);
    }

    static void CheckLoadThenModify(List<EntityEntry> entries)
    {
        var entityType = entries[0].Metadata;
        if (entries.Any(_ => _.Metadata != entityType))
        {
            return;
        }

        var name = entityType.DisplayName();
        if (entries.All(_ => _.State == EntityState.Deleted))
        {
            throw new(
                $"""
                 SaveChanges deleted {entries.Count} {name} entities, and made no other change.
                 ExecuteDeleteAsync deletes them in one statement, without loading them.
                 """);
        }

        if (entries.Any(_ => _.State != EntityState.Modified))
        {
            return;
        }

        var properties = ModifiedProperties(entries[0]);
        if (entries.Any(_ => ModifiedProperties(_) != properties))
        {
            return;
        }

        throw new(
            $"""
             SaveChanges changed {properties} on {entries.Count} {name} entities, and made no other change.
             ExecuteUpdateAsync updates them in one statement, without loading them.
             """);
    }

    static string ModifiedProperties(EntityEntry entry) =>
        string.Join(
            ", ",
            entry.Properties
                .Where(_ => _.IsModified)
                .Select(_ => _.Metadata.Name));
}
