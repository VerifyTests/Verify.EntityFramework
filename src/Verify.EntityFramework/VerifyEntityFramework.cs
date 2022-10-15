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

    public static void IgnoreNavigationProperties(this VerifySettings settings, DbContext context) =>
        settings.IgnoreNavigationProperties(context.Model);

    public static SettingsTask IgnoreNavigationProperties(this SettingsTask settings, DbContext context) =>
        settings.IgnoreNavigationProperties(context.Model);

    public static SettingsTask IgnoreNavigationProperties(this SettingsTask settings, IModel model)
    {
        foreach (var (type, name) in model.GetNavigations())
        {
            settings.IgnoreMember(type, name);
        }

        return settings;
    }

    public static void IgnoreNavigationProperties(this VerifySettings settings, IModel model)
    {
        foreach (var (type, name) in model.GetNavigations())
        {
            settings.IgnoreMember(type, name);
        }
    }

    public static void IgnoreNavigationProperties(IModel model)
    {
        foreach (var (type, name) in model.GetNavigations())
        {
            VerifierSettings.IgnoreMember(type, name);
        }
    }

    static IEnumerable<(Type type, string name)> GetNavigations(this IModel model)
    {
        foreach (var type in model.GetEntityTypes())
        {
            foreach (var navigation in type.GetNavigations())
            {
                yield return new(type.ClrType, navigation.Name);
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
            (target, _) => QueryableConverter.IsQueryable(target));

        VerifierSettings.IgnoreMembersWithType(typeof(IDbContextFactory<>));
        VerifierSettings.IgnoreMembersWithType<DbContext>();
        VerifierSettings.AddExtraSettings(serializer =>
        {
            var converters = serializer.Converters;
            converters.Add(new DbUpdateExceptionConverter());
            converters.Add(new TrackerConverter());
            converters.Add(new QueryableConverter());
        });
    }

    static ConversionResult QueryableToSql(object arg, IReadOnlyDictionary<string, object> context)
    {
        var sql = QueryableConverter.QueryToSql(arg);
        return new(null, "txt", sql);
    }
}