namespace VerifyTests;

/// <summary>
/// Opt in anti-pattern checks. Each is off by default.
/// </summary>
public sealed class AntiPatternOptions
{
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
    /// Throw when ToLower, ToUpper, ToLowerInvariant, or ToUpperInvariant is called on a column in a filter, ordering,
    /// join, or predicate, which stops the database using an index on it. Opt in, since whether the conversion is
    /// needed depends on the column's collation: SQL Server's default is case insensitive, but many databases are case
    /// sensitive.
    /// </summary>
    public bool ThrowOnColumnCaseConversion { get; set; }

    /// <summary>
    /// Throw when a query Includes a collection, and also filters by that collection in a Where, for example
    /// <c>Include(_ => _.Employees).Where(_ => _.Employees.Any(...))</c>. The Where filters the parents, but the
    /// Include still loads every child, which is often meant as a filtered Include. Opt in, since filtering the parents
    /// by their children is also a correct query.
    /// </summary>
    public bool ThrowOnCollectionFilterOutsideInclude { get; set; }

    internal AntiPatternOptions Clone() =>
        (AntiPatternOptions) MemberwiseClone();
}
