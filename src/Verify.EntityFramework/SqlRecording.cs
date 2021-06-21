using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace VerifyTests.EntityFramework
{
    public static class EfRecording
    {
        public static void EnableRecording(this DbContextOptionsBuilder builder)
        {
            Guard.AgainstNull(builder, nameof(builder));
            builder.AddInterceptors(new LogCommandInterceptor());
        }

        public static void StartRecording()
        {
            LogCommandInterceptor.Start();
        }

        public static IEnumerable<LogEntry> FinishRecording()
        {
            var entries = LogCommandInterceptor.Stop();
            if (entries != null)
            {
                return entries;
            }
            throw new("No recorded state. It is possible `VerifyEntityFramework.StartRecording()` has not been called on the DbContext.");
        }
    }
}