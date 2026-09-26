// InMemory executes no DbCommand, so LogCommandInterceptor never sees its queries. They are recorded here instead,
// once EF has extracted the parameters. An IQueryExpressionInterceptor cannot be used, since it only sees a query
// the first time it is compiled, and compiled queries are cached.
class RecordingQueryCompiler(
    IQueryContextFactory queryContextFactory,
    ICompiledQueryCache compiledQueryCache,
    ICompiledQueryCacheKeyGenerator compiledQueryCacheKeyGenerator,
    IDatabase database,
    IDiagnosticsLogger<DbLoggerCategory.Query> logger,
    ICurrentDbContext currentContext,
    IEvaluatableExpressionFilter evaluatableExpressionFilter,
    IModel model,
    IDbContextOptions options) :
        QueryCompiler(queryContextFactory,
            compiledQueryCache,
            compiledQueryCacheKeyGenerator,
            database,
            logger,
            currentContext,
            evaluatableExpressionFilter,
            model)
{
    DbContext context = currentContext.Context;
    LogCommandInterceptor? interceptor = options.FindExtension<RecordingOptionsExtension>()?.Interceptor;
    bool? isInMemory;
    string type = "Query";

    public override TResult Execute<TResult>(Expression query)
    {
        type = "Query";
        return base.Execute<TResult>(query);
    }

    public override TResult ExecuteAsync<TResult>(Expression query, Cancel cancel = default)
    {
        type = "QueryAsync";
        return base.ExecuteAsync<TResult>(query, cancel);
    }

    public override Expression ExtractParameters(
        Expression query,
        Dictionary<string, object?> parameters,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger,
        bool compiledQuery = false,
        bool generateContextAccessors = false)
    {
        // read before extracting, since extracting can evaluate code that runs another query
        var entryType = type;
        var extracted = base.ExtractParameters(
            query,
            parameters,
            logger,
            compiledQuery,
            generateContextAccessors);

        // a compiled query is extracted once, when compiled, and then executes without passing through here
        if (!compiledQuery &&
            interceptor != null &&
            (isInMemory ??= context.IsInMemory()))
        {
            interceptor.AddQuery(entryType, context, extracted, parameters);
        }

        return extracted;
    }
}
