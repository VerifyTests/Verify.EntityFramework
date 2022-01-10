using Microsoft.EntityFrameworkCore;

namespace VerifyTests.EntityFramework;

public static class EfRecording
{
    public static void EnableRecording(this DbContextOptionsBuilder builder)
    {
        builder.AddInterceptors(new LogCommandInterceptor());
    }

    public static void StartRecording()
    {
        LogCommandInterceptor.Start();
    }

    public static IEnumerable<LogEntry> FinishRecording()
    {
        var entries = LogCommandInterceptor.Stop();
        if (entries is not null)
        {
            return entries;
        }
        throw new("No recorded state. It is possible `VerifyEntityFramework.StartRecording()` has not been called on the DbContext.");
    }
}