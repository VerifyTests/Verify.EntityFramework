public sealed class LocalDbSqlDatabase<TDbContext>(SqlDatabase<TDbContext> sqlDatabase) : ISqlDatabase<TDbContext>
    where TDbContext : DbContext
{
    public string ConnectionString { get; } = sqlDatabase.ConnectionString;

    public TDbContext Context { get; } = sqlDatabase.Context;

    public TDbContext NewDbContext() => sqlDatabase.NewDbContext();

    public Task AddData(params object[] entities) => sqlDatabase.AddData(entities);

    public ValueTask DisposeAsync() => sqlDatabase.DisposeAsync();
}