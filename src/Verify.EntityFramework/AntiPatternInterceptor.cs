// Checks each query for anti-patterns when it is compiled. A query that throws is not added to the compiled query
// cache, so it throws every time it is executed. Registered by AntiPatternOptionsExtension.
class AntiPatternInterceptor :
    IQueryExpressionInterceptor
{
    public static AntiPatternInterceptor Instance { get; } = new();

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
        CoreEventId.LazyLoadOnDisposedContextWarning,
        CoreEventId.DetachedLazyLoadingWarning,
        // model anti-patterns, logged when the model is built
        RelationalEventId.BoolWithDefaultWarning,
        RelationalEventId.ModelValidationKeyDefaultValueWarning,
        RelationalEventId.OptionalDependentWithoutIdentifyingPropertyWarning
    ];

    public Expression QueryCompilationStarting(Expression query, QueryExpressionEventData data)
    {
        DiscardedOrderByDetector.ThrowIfDiscarded(query);
        RedundantNullCheckDetector.ThrowIfRedundant(query);
        CountComparisonDetector.ThrowIfCountCompared(query);
        GroupByKeyDetector.ThrowIfOnlyKey(query);

        var context = data.Context;
        if (context != null)
        {
            IgnoredEntityOperatorDetector.ThrowIfIgnored(query, context.Model);
            IgnoredQuerySplittingDetector.ThrowIfIgnored(query, context.Model);
            RedundantDistinctDetector.ThrowIfRedundant(query, context.Model);

            var options = context.GetService<IDbContextOptions>().FindExtension<AntiPatternOptionsExtension>()?.Options;
            if (options is { ThrowOnColumnCaseConversion: true })
            {
                CaseConversionDetector.ThrowIfColumnConverted(query);
            }
        }

        return query;
    }
}
