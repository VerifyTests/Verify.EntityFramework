public interface ISqlDatabase<out TDbContext> : IAsyncDisposable
    where TDbContext : DbContext
{
    string ConnectionString { get; }

    TDbContext Context { get; }

    TDbContext NewDbContext();

    Task AddData(params object[] entities);
}