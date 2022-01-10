using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace VerifyTests;

public static class VerifyEntityFramework
{
    public static async IAsyncEnumerable<object> AllData(this DbContext data)
    {
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

    public static void IgnoreNavigationProperties(this SerializationSettings settings, DbContext context)
    {
        IgnoreNavigationProperties(settings, context.Model);
    }

    public static void IgnoreNavigationProperties(this SerializationSettings settings, IModel model)
    {
        foreach (var type in model.GetEntityTypes())
        {
            foreach (var navigation in type.GetNavigations())
            {
                settings.IgnoreMember(type.ClrType, navigation.Name);
            }
        }
    }

    public static void Enable()
    {
        VerifierSettings.RegisterJsonAppender(_ =>
        {
            var entries = LogCommandInterceptor.Stop();
            if (entries is null)
            {
                return null;
            }

            return new ToAppend("sql", entries);
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
        return new(null, "txt", sql);
    }
}