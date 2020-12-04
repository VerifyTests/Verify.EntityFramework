using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace VerifyTests
{
    public static class VerifyEntityFramework
    {
        public static async IAsyncEnumerable<object> AllData(this DbContext data)
        {
            Guard.AgainstNull(data, nameof(data));

            foreach (var entityType in data.EntityTypes().Where(p => !p.IsOwned()))
            {
                var clrType = entityType.ClrType;
                var set = data.Set(clrType);
                var queryable = set.AsNoTracking(clrType);
                foreach (var entity in await queryable.ToListAsync())
                {
                    yield return entity;
                }
            }
        }

        public static void Enable()
        {
            VerifierSettings.RegisterJsonAppender(_ =>
            {
                var entries = LogCommandInterceptor.Stop();
                if (entries == null)
                {
                    return null;
                }

                return new("sql", entries);
            });

            VerifierSettings.RegisterFileConverter(
                QueryableToSql,
                (target, _, _) => QueryableConverter.IsQueryable(target));

            VerifierSettings.ModifySerialization(settings =>
            {
                settings.AddExtraSettings(serializer =>
                {
                    var converters = serializer.Converters;
                    converters.Add(new TrackerConverter());
                    converters.Add(new QueryableConverter());
                });
            });
        }

        static ConversionResult QueryableToSql(object arg, IReadOnlyDictionary<string, object> context)
        {
            var sql = QueryableConverter.QueryToSql(arg);
            return new(
                null,
                new[]
                {
                    new ConversionStream("txt", StringToMemoryStream(sql)),
                });
        }

        static MemoryStream StringToMemoryStream(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text.Replace("\r\n", "\n"));
            return new(bytes);
        }
    }
}