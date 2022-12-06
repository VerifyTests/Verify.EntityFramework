namespace VerifyTests.EntityFramework;

public static class EfRecording
{
    public static DbContextOptionsBuilder<TContext> EnableRecording<TContext>(this DbContextOptionsBuilder<TContext> builder)
        where TContext : DbContext
        => builder.EnableRecording(null);

    public static DbContextOptionsBuilder<TContext> EnableRecording<TContext>(this DbContextOptionsBuilder<TContext> builder, string? identifier)
        where TContext : DbContext
        => builder.AddInterceptors(new LogCommandInterceptor(identifier));

    public static void StartRecording()
        => LogCommandInterceptor.Start();

    public static void StartRecording(string identifier)
        => LogCommandInterceptor.Start(identifier);

    public static IReadOnlyList<LogEntry> FinishRecording()
    {
        var entries = LogCommandInterceptor.Stop();
        if (entries is not null)
        {
            return entries;
        }

        throw new("No recorded state. It is possible `VerifyEntityFramework.StartRecording()` has not been called on the DbContext.");
    }

    public static IReadOnlyList<LogEntry> FinishRecording(string identifier)
    {
        var entries = LogCommandInterceptor.Stop(identifier);
        if (entries is not null)
        {
            return entries;
        }

        throw new("No recorded state. It is possible `VerifyEntityFramework.StartRecording(string identifier)` has not been called on the DbContext.");
    }
}