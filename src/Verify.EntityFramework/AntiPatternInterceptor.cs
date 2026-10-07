// Checks each query for the anti-patterns selected in AntiPatternOptions when it is compiled. A query that throws is not added to the compiled query
// cache, so it throws every time it is executed. Registered by AntiPatternOptionsExtension.
class AntiPatternInterceptor :
    IQueryExpressionInterceptor
{
    public static AntiPatternInterceptor Instance { get; } = new();

    // SqlServerEventId.DecimalTypeDefaultWarning. Built from its id and name, since this library does not reference
    // the SQL Server provider. Warnings are configured by id, and other providers never log it. Declared before
    // Warnings, since static initializers run in order.
    internal static Microsoft.Extensions.Logging.EventId DecimalTypeDefaultWarning { get; } =
        new(30000, "Microsoft.EntityFrameworkCore.Model.Validation.DecimalTypeDefaultWarning");

    // anti-patterns that EF detects, but only logs. Thrown for ThrowOnEfWarnings
    public static Microsoft.Extensions.Logging.EventId[] Warnings { get; } =
    [
        RelationalEventId.MultipleCollectionIncludeWarning,
        CoreEventId.RowLimitingOperationWithoutOrderByWarning,
        CoreEventId.FirstWithoutOrderByAndFilterWarning,
        CoreEventId.DistinctAfterOrderByWithoutRowLimitingOperatorWarning,
        CoreEventId.PossibleUnintendedReferenceComparisonWarning,
        CoreEventId.PossibleUnintendedCollectionNavigationNullComparisonWarning,
        RelationalEventId.QueryPossibleUnintendedUseOfEqualsWarning,
        CoreEventId.NavigationBaseIncludeIgnored,
        // model anti-patterns, logged when the model is built
        RelationalEventId.BoolWithDefaultWarning,
        RelationalEventId.ModelValidationKeyDefaultValueWarning,
        RelationalEventId.OptionalDependentWithoutIdentifyingPropertyWarning,
        CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning,
        DecimalTypeDefaultWarning,
        // logged by SaveChanges, when an optional dependent with only null values is not saved
        RelationalEventId.OptionalDependentWithAllNullPropertiesWarning
    ];

    // Only thrown for ThrowOnLazyLoading, not ThrowOnEfWarnings. Verify reads every navigation when it serializes an entity, so
    // NavigationLazyLoading would throw from inside Verify, not the code under test. LazyLoadOnDisposedContextWarning
    // and DetachedLazyLoadingWarning are listed for completeness: EF already throws for them by default.
    public static Microsoft.Extensions.Logging.EventId[] LazyLoadingWarnings { get; } =
    [
        CoreEventId.NavigationLazyLoading,
        CoreEventId.LazyLoadOnDisposedContextWarning,
        CoreEventId.DetachedLazyLoadingWarning
    ];

    public Expression QueryCompilationStarting(Expression query, QueryExpressionEventData data)
    {
        if (InternalQuery.Is(query))
        {
            return query;
        }

        var context = data.Context;
        var options = context?.GetService<IDbContextOptions>().FindExtension<AntiPatternOptionsExtension>()?.Options;
        if (options == null)
        {
            return query;
        }

        if (options.ThrowOnDiscardedOrderBy)
        {
            DiscardedOrderByDetector.ThrowIfDiscarded(query);
        }

        if (options.ThrowOnConstantOrdering)
        {
            ConstantOrderingDetector.ThrowIfConstant(query);
        }

        if (options.ThrowOnRedundantNullCheck)
        {
            RedundantNullCheckDetector.ThrowIfRedundant(query);
        }

        if (options.ThrowOnCountComparison)
        {
            CountComparisonDetector.ThrowIfCountCompared(query);
        }

        if (options.ThrowOnGroupByOnlyKey)
        {
            GroupByKeyDetector.ThrowIfOnlyKey(query);
        }

        var model = context!.Model;
        if (options.ThrowOnIgnoredEntityOperators)
        {
            IgnoredEntityOperatorDetector.ThrowIfIgnored(query, model);
        }

        if (options.ThrowOnIgnoredQuerySplitting)
        {
            IgnoredQuerySplittingDetector.ThrowIfIgnored(query, model);
        }

        if (options.ThrowOnRedundantDistinct)
        {
            RedundantDistinctDetector.ThrowIfRedundant(query, model);
        }

        if (options.ThrowOnUnorderedFirst)
        {
            UnorderedFirstDetector.ThrowIfUnordered(query, model);
        }

        if (options.ThrowOnRedundantInclude)
        {
            RedundantIncludeDetector.ThrowIfRedundant(query, model);
        }

        if (options.ThrowOnRequiredNullCheck)
        {
            RequiredNullCheckDetector.ThrowIfRedundant(query, model);
        }

        if (options.ThrowOnColumnCaseConversion)
        {
            CaseConversionDetector.ThrowIfColumnConverted(query);
        }

        if (options.ThrowOnCollectionFilterOutsideInclude)
        {
            CollectionFilterOutsideIncludeDetector.ThrowIfFilteredOutside(query);
        }

        if (options.ThrowOnIgnoredQueryFilters)
        {
            IgnoredQueryFiltersDetector.ThrowIfIgnored(query, model);
        }

        return query;
    }
}
