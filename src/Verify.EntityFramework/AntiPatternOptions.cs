namespace VerifyTests;

/// <summary>
/// The anti-pattern checks to run. Each is off by default. <see cref="EnableStandard" /> switches on the standard set,
/// which is what ThrowOnAntiPatterns does when it is called with no configure action.
/// </summary>
public sealed class AntiPatternOptions
{
    /// <summary>
    /// Switch on the standard set of checks: those that find a query, or a model, that is written wrong whatever data
    /// it runs against. That is <see cref="ThrowOnIgnoredEntityOperators" />, <see cref="ThrowOnDiscardedOrderBy" />,
    /// <see cref="ThrowOnConstantOrdering" />, <see cref="ThrowOnIgnoredQuerySplitting" />,
    /// <see cref="ThrowOnRedundantInclude" />, <see cref="ThrowOnRequiredNullCheck" />,
    /// <see cref="ThrowOnRedundantNullCheck" />, <see cref="ThrowOnCountComparison" />,
    /// <see cref="ThrowOnRedundantDistinct" />, <see cref="ThrowOnGroupByOnlyKey" />,
    /// <see cref="ThrowOnUnorderedFirst" />, and <see cref="ThrowOnEfWarnings" />. The other checks are not changed.
    /// </summary>
    public void EnableStandard()
    {
        ThrowOnIgnoredEntityOperators = true;
        ThrowOnDiscardedOrderBy = true;
        ThrowOnConstantOrdering = true;
        ThrowOnIgnoredQuerySplitting = true;
        ThrowOnRedundantInclude = true;
        ThrowOnRequiredNullCheck = true;
        ThrowOnRedundantNullCheck = true;
        ThrowOnCountComparison = true;
        ThrowOnRedundantDistinct = true;
        ThrowOnGroupByOnlyKey = true;
        ThrowOnUnorderedFirst = true;
        ThrowOnEfWarnings = true;
    }

    /// <summary>
    /// Throw for an Include, ThenInclude, or tracking option that EF ignores, since the query returns no entity, for
    /// example it ends in a projection, or a scalar like Count. Also for AsNoTracking or AsTracking on a query that
    /// only returns a keyless entity type.
    /// </summary>
    public bool ThrowOnIgnoredEntityOperators { get; set; }

    /// <summary>
    /// Throw for an ordering that EF discards, since it is followed by another OrderBy, or by an operator whose result
    /// does not depend on order, like Count, Any, Single, GroupBy, or ExecuteDelete.
    /// </summary>
    public bool ThrowOnDiscardedOrderBy { get; set; }

    /// <summary>
    /// Throw for an ordering by a value that is the same for every row, for example <c>OrderBy(_ => 1)</c>, which does
    /// not order the rows.
    /// </summary>
    public bool ThrowOnConstantOrdering { get; set; }

    /// <summary>
    /// Throw for AsSplitQuery or AsSingleQuery on a query that loads no collection.
    /// </summary>
    public bool ThrowOnIgnoredQuerySplitting { get; set; }

    /// <summary>
    /// Throw for an Include of an owned or AutoInclude navigation, or whose path is the same as, or the start of,
    /// another Include path.
    /// </summary>
    public bool ThrowOnRedundantInclude { get; set; }

    /// <summary>
    /// Throw for a null check of a required property or navigation, for example <c>_.Name != null</c>, which is always
    /// true.
    /// </summary>
    public bool ThrowOnRequiredNullCheck { get; set; }

    /// <summary>
    /// Throw for a redundant null check, on a navigation or a nullable scalar, for example
    /// <c>_.Owner != null &amp;&amp; _.Owner.Name == "owner"</c>, or <c>_.Owner == null ? null : _.Owner.Name</c>.
    /// </summary>
    public bool ThrowOnRedundantNullCheck { get; set; }

    /// <summary>
    /// Throw for a count compared to zero, for example <c>_.Employees.Count() &gt; 0</c>, where Any() stops at the
    /// first row.
    /// </summary>
    public bool ThrowOnCountComparison { get; set; }

    /// <summary>
    /// Throw for a redundant Distinct, on rows that each come from one entity and include its primary key.
    /// </summary>
    public bool ThrowOnRedundantDistinct { get; set; }

    /// <summary>
    /// Throw for a GroupBy whose groups are only used for their Key, which is Select(key).Distinct().
    /// </summary>
    public bool ThrowOnGroupByOnlyKey { get; set; }

    /// <summary>
    /// Throw for First or FirstOrDefault on a collection navigation in a subquery, without an ordering or a filter, for
    /// example <c>_.Employees.FirstOrDefault()</c>, which returns an arbitrary element.
    /// </summary>
    public bool ThrowOnUnorderedFirst { get; set; }

    /// <summary>
    /// Throw for the query and model anti-patterns that EF detects but only logs, for example Take without OrderBy, or
    /// a decimal with no precision. To allow one, call ConfigureWarnings after ThrowOnAntiPatterns.
    /// </summary>
    public bool ThrowOnEfWarnings { get; set; }

    /// <summary>
    /// Only run the runtime checks (synchronous calls, repeated queries, repeated SaveChanges, single row saves, and
    /// load then modify) while Verify is recording, so the setup and assertions of a test are not counted. Uses the
    /// recording that EnableRecording set up, including its identifier, or otherwise the default recording. Defaults to
    /// true. Set to false to check everything a context does.
    /// </summary>
    public bool OnlyWhileRecording { get; set; } = true;

    /// <summary>
    /// Throw when a navigation is lazy loaded, or when lazy loading does nothing, since the entity is detached. Each
    /// lazy load is a separate query, so a loop that reads a navigation runs one query per item. Applies whether or not
    /// Verify is recording. Verify reads every navigation when it serializes an entity, so use
    /// IgnoreNavigationProperties when verifying entities that lazy load. EF itself throws, by default, for a lazy load
    /// after the context is disposed.
    /// </summary>
    public bool ThrowOnLazyLoading { get; set; }

    /// <summary>
    /// Throw when a query, a SaveChanges, or raw SQL executes synchronously. Commands that EF runs itself, like
    /// migrations, are not checked. For InMemory, which executes no commands, only SaveChanges is checked.
    /// </summary>
    public bool ThrowOnSynchronousCalls { get; set; }

    /// <summary>
    /// Throw when one context executes the same query SQL more than <see cref="RepeatedQueryThreshold" /> times,
    /// which usually means a query in a loop (N+1). Relational providers only.
    /// </summary>
    public bool ThrowOnRepeatedQueries { get; set; }

    /// <summary>
    /// Defaults to 2, since test data is usually small, and the count only covers the code under test.
    /// </summary>
    public int RepeatedQueryThreshold { get; set; } = 2;

    /// <summary>
    /// Throw when one context saves changes more than <see cref="RepeatedSaveChangesThreshold" /> times, which
    /// usually means SaveChanges in a loop.
    /// </summary>
    public bool ThrowOnRepeatedSaveChanges { get; set; }

    /// <summary>
    /// Defaults to 2, since test data is usually small, and the count only covers the code under test.
    /// </summary>
    public int RepeatedSaveChangesThreshold { get; set; } = 2;

    /// <summary>
    /// Throw when one context has more than <see cref="SingleRowSavesThreshold" /> SaveChanges calls that each save a
    /// single entity, which usually means adding and saving one entity per iteration of a loop.
    /// </summary>
    public bool ThrowOnSingleRowSaves { get; set; }

    /// <summary>
    /// Defaults to 2, since test data is usually small, and the count only covers the code under test.
    /// </summary>
    public int SingleRowSavesThreshold { get; set; } = 2;

    /// <summary>
    /// Throw when a SaveChanges only deletes, or only makes the same change to, more than
    /// <see cref="LoadThenModifyThreshold" /> entities of one type. ExecuteDelete or ExecuteUpdate does that in one
    /// statement, without loading the entities.
    /// </summary>
    public bool ThrowOnLoadThenModify { get; set; }

    /// <summary>
    /// Defaults to 1, so it fires from 2 entities, since loading entities only to delete or update them is the
    /// anti-pattern, however many there are.
    /// </summary>
    public int LoadThenModifyThreshold { get; set; } = 1;

    /// <summary>
    /// Throw when SaveChanges updates every column of an entity that a tracking query loaded, though some are unchanged.
    /// That is what Update(), or setting State to Modified, does to an entity the context already tracks. Change tracking
    /// already detects the changes, and without the call only the changed columns are written.
    /// </summary>
    public bool ThrowOnRedundantUpdate { get; set; }

    /// <summary>
    /// Throw when ToLower, ToUpper, ToLowerInvariant, or ToUpperInvariant is called on a column in a filter, ordering,
    /// join, or predicate, which stops the database using an index on it. Not in the standard set, since whether the
    /// conversion is needed depends on the column's collation: SQL Server's default is case insensitive, but many
    /// databases are case sensitive.
    /// </summary>
    public bool ThrowOnColumnCaseConversion { get; set; }

    /// <summary>
    /// Throw when a query Includes a collection, and also filters by that collection in a Where, for example
    /// <c>Include(_ => _.Employees).Where(_ => _.Employees.Any(...))</c>. The Where filters the parents, but the
    /// Include still loads every child, which is often meant as a filtered Include. Not in the standard set, since
    /// filtering the parents by their children is also a correct query.
    /// </summary>
    public bool ThrowOnCollectionFilterOutsideInclude { get; set; }

    /// <summary>
    /// Throw when IgnoreQueryFilters is called on a query where no entity type has a query filter, or none with the
    /// given keys, so it does nothing. Not in the standard set, since code that is shared between entity types, for
    /// example a generic helper, can not know whether the entity type it is given has a query filter.
    /// </summary>
    public bool ThrowOnIgnoredQueryFilters { get; set; }

    // The checks that run while a query is compiled. Part of the service provider identity, see
    // AntiPatternOptionsExtension.
    internal int CompileFlags
    {
        get
        {
            bool[] flags =
            [
                ThrowOnIgnoredEntityOperators,
                ThrowOnDiscardedOrderBy,
                ThrowOnConstantOrdering,
                ThrowOnIgnoredQuerySplitting,
                ThrowOnRedundantInclude,
                ThrowOnRequiredNullCheck,
                ThrowOnRedundantNullCheck,
                ThrowOnCountComparison,
                ThrowOnRedundantDistinct,
                ThrowOnGroupByOnlyKey,
                ThrowOnUnorderedFirst,
                ThrowOnColumnCaseConversion,
                ThrowOnCollectionFilterOutsideInclude,
                ThrowOnIgnoredQueryFilters
            ];
            var result = 0;
            for (var index = 0; index < flags.Length; index++)
            {
                if (flags[index])
                {
                    result |= 1 << index;
                }
            }

            return result;
        }
    }

    internal AntiPatternOptions Clone() =>
        (AntiPatternOptions) MemberwiseClone();
}
