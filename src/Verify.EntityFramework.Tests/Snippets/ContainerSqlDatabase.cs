public sealed class ContainerSqlDatabase<TDbContext>(Func<TDbContext> dbContext) : ISqlDatabase<TDbContext>
    where TDbContext : DbContext
{
    public string ConnectionString => Context.Database.GetConnectionString()!;

    public TDbContext Context { get; } = dbContext();

    public TDbContext NewDbContext() => dbContext();

    public Task AddData(params object[] entities) => Context.AddData(entities);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}