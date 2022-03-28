using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace VerifyTests.EntityFramework;

public static class EfRecording
{
    public static DbContextOptionsBuilder<TContext> EnableRecording<TContext>(this DbContextOptionsBuilder<TContext> builder)
        where TContext : DbContext
        => builder.AddInterceptors(new LogCommandInterceptor());

    public static DbContextOptionsBuilder<TContext> EnableRecording<TContext>(this DbContextOptionsBuilder<TContext> builder, string instanceName)
        where TContext : DbContext
    {
        ((IDbContextOptionsBuilderInfrastructure)builder)
            .AddOrUpdateExtension(
                GetOrCreateExtension(builder, instanceName)
                    .WithInstanceName(instanceName));

        return builder.EnableRecording();

        static VerifyEfOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder, string instanceName)
            => optionsBuilder.Options.FindExtension<VerifyEfOptionsExtension>()
                ?? new VerifyEfOptionsExtension();
    }

    public static void StartRecording()
        => LogCommandInterceptor.Start();

    public static void StartRecording(string identifier)
        => LogCommandInterceptor.Start(identifier);

    public static IEnumerable<LogEntry> FinishRecording()
    {
        var entries = LogCommandInterceptor.Stop();
        if (entries is not null)
        {
            return entries;
        }
        throw new("No recorded state. It is possible `VerifyEntityFramework.StartRecording()` has not been called on the DbContext.");
    }

    public static IEnumerable<LogEntry> FinishRecording(string identifier)
    {
        var entries = LogCommandInterceptor.Stop(identifier);
        if (entries is not null)
        {
            return entries;
        }
        throw new("No recorded state. It is possible `VerifyEntityFramework.StartRecording(string identifier)` has not been called on the DbContext.");
    }
}