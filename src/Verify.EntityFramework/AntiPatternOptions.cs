namespace VerifyTests;

/// <summary>
/// Opt in anti-pattern checks. Each is off by default.
/// </summary>
public sealed class AntiPatternOptions
{
    /// <summary>
    /// Throw when a navigation is lazy loaded. Each lazy load is a separate query, so a loop that reads a
    /// navigation runs one query per item.
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

    public int RepeatedQueryThreshold { get; set; } = 10;

    /// <summary>
    /// Throw when one context saves changes more than <see cref="RepeatedSaveChangesThreshold" /> times, which
    /// usually means SaveChanges in a loop.
    /// </summary>
    public bool ThrowOnRepeatedSaveChanges { get; set; }

    public int RepeatedSaveChangesThreshold { get; set; } = 10;

    /// <summary>
    /// Throw when one context has more than <see cref="SingleRowSavesThreshold" /> SaveChanges calls that each save a
    /// single entity, which usually means adding and saving one entity per iteration of a loop.
    /// </summary>
    public bool ThrowOnSingleRowSaves { get; set; }

    public int SingleRowSavesThreshold { get; set; } = 10;

    /// <summary>
    /// Throw when a SaveChanges only deletes, or only makes the same change to, more than
    /// <see cref="LoadThenModifyThreshold" /> entities of one type. ExecuteDelete or ExecuteUpdate does that in one
    /// statement, without loading the entities.
    /// </summary>
    public bool ThrowOnLoadThenModify { get; set; }

    public int LoadThenModifyThreshold { get; set; } = 10;

    /// <summary>
    /// Throw, when a context is disposed, if it loaded entities with a tracking query but never saved a change.
    /// AsNoTracking would have avoided the tracking cost. Not checked for pooled contexts, which are not disposed.
    /// </summary>
    public bool ThrowOnUnmodifiedTracking { get; set; }

    /// <summary>
    /// Throw when ToLower, ToUpper, ToLowerInvariant, or ToUpperInvariant is called on a column in a filter, ordering,
    /// join, or predicate, which stops the database using an index on it. Opt in, since whether the conversion is
    /// needed depends on the column's collation: SQL Server's default is case insensitive, but many databases are case
    /// sensitive.
    /// </summary>
    public bool ThrowOnColumnCaseConversion { get; set; }

    internal AntiPatternOptions Clone() =>
        (AntiPatternOptions) MemberwiseClone();
}
