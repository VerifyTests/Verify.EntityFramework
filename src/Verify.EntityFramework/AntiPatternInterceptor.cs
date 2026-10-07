// Checks each query for anti-patterns when it is compiled. A query that throws is not added to the compiled query
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

    // anti-patterns that EF detects, but only logs
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

    // Only thrown for ThrowOnLazyLoading, not by default. Verify reads every navigation when it serializes an entity, so
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

        DiscardedOrderByDetector.ThrowIfDiscarded(query);
        ConstantOrderingDetector.ThrowIfConstant(query);
        RedundantNullCheckDetector.ThrowIfRedundant(query);
        CountComparisonDetector.ThrowIfCountCompared(query);
        GroupByKeyDetector.ThrowIfOnlyKey(query);

        var context = data.Context;
        if (context != null)
        {
            IgnoredEntityOperatorDetector.ThrowIfIgnored(query, context.Model);
            IgnoredQuerySplittingDetector.ThrowIfIgnored(query, context.Model);
            RedundantDistinctDetector.ThrowIfRedundant(query, context.Model);
            UnorderedFirstDetector.ThrowIfUnordered(query, context.Model);
            RedundantIncludeDetector.ThrowIfRedundant(query, context.Model);
            RequiredNullCheckDetector.ThrowIfRedundant(query, context.Model);

            var options = context.GetService<IDbContextOptions>().FindExtension<AntiPatternOptionsExtension>()?.Options;
            if (options is { ThrowOnColumnCaseConversion: true })
            {
                CaseConversionDetector.ThrowIfColumnConverted(query);
            }

            if (options is { ThrowOnCollectionFilterOutsideInclude: true })
            {
                CollectionFilterOutsideIncludeDetector.ThrowIfFilteredOutside(query);
            }

            if (options is { ThrowOnIgnoredQueryFilters: true })
            {
                IgnoredQueryFiltersDetector.ThrowIfIgnored(query, context.Model);
            }
        }

        return query;
    }
}
