﻿namespace VerifyTests;

public static class VerifyEntityFramework
{
    static List<(Type type, string name)>? modelNavigations;

    public static async IAsyncEnumerable<object> AllData(this DbContext data)
    {
        foreach (var entityType in data
                     .EntityTypes()
                     .OrderBy(_ => _.Name)
                     .Where(_ => !_.IsOwned()))
        {
            var clrType = entityType.ClrType;
            var set = data.Set(clrType);
            var queryable = set.AsNoTracking(clrType);

            IEnumerable<object> list = await queryable.ToListAsync();
            var idProperty = clrType.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (idProperty != null)
            {
                list = list.OrderBy(_ => idProperty.GetValue(_));
            }

            foreach (var entity in list)
            {
                yield return entity;
            }
        }
    }

    public static void IgnoreNavigationProperties(this VerifySettings settings, DbContext context) =>
        settings.IgnoreNavigationProperties(context.Model);

    public static SettingsTask IgnoreNavigationProperties(this SettingsTask settings, DbContext context) =>
        settings.IgnoreNavigationProperties(context.Model);

    public static SettingsTask IgnoreNavigationProperties(this SettingsTask settings, IModel? model = null)
    {
        foreach (var (type, name) in model.GetNavigationsOrShared())
        {
            settings.IgnoreMember(type, name);
        }

        return settings;
    }

    public static void IgnoreNavigationProperties(this VerifySettings settings, IModel? model = null)
    {
        foreach (var (type, name) in model.GetNavigationsOrShared())
        {
            settings.IgnoreMember(type, name);
        }
    }

    public static void ScrubInlineEfDateTimes() =>
        VerifierSettings.ScrubInlineDateTimes("yyyy-MM-ddTHH:mm:ss.fffffffZ");

    public static SettingsTask ScrubInlineEfDateTimes(this SettingsTask settings)
    {
        settings.CurrentSettings.ScrubInlineEfDateTimes();
        return settings;
    }

    public static void ScrubInlineEfDateTimes(this VerifySettings settings) =>
        settings.ScrubInlineDateTimes("yyyy-MM-ddTHH:mm:ss.fffffffZ");

    public static void IgnoreNavigationProperties(IModel? model = null)
    {
        foreach (var (type, name) in model.GetNavigationsOrShared())
        {
            VerifierSettings.IgnoreMember(type, name);
        }
    }

    static IEnumerable<(Type type, string name)> GetNavigationsOrShared(this IModel? model)
    {
        if (model == null)
        {
            if (modelNavigations != null)
            {
                return modelNavigations;
            }

            throw new("The `model` parameter must be provided wither on this method or on VerifyEntityFramework.Enable()");
        }

        return GetNavigations(model);
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

    public static void Initialize(DbContext context) =>
        Initialize(context.Model);

    public static bool Initialized { get; private set; }

    public static void Initialize(IModel? model = null)
    {
        if (Initialized)
        {
            throw new("Already Initialized");
        }

        Initialized = true;

        InnerVerifier.ThrowIfVerifyHasBeenRun();
        if (model != null)
        {
            modelNavigations = model
                .GetNavigations()
                .ToList();
        }

        VerifierSettings.RegisterFileConverter(
            QueryableToSql,
            (target, _) => QueryableConverter.IsQueryable(target));
        var formatSql = model != null && model.IsSqlServer();
        VerifierSettings.IgnoreMembersWithType(typeof(IDbContextFactory<>));
        VerifierSettings.IgnoreMembersWithType<DbContext>();
        var converters = DefaultContractResolver.Converters;
        converters.Add(new DbUpdateExceptionConverter());
        converters.Add(new TrackerConverter());
        converters.Add(new QueryableConverter(formatSql));
        converters.Add(new LogEntryConverter());
    }
    static bool IsSqlServer(this IModel model)
    {
        var dependencies = model.ModelDependencies;
        var mappingSourceName = dependencies?.TypeMappingSource.GetType().Name;
        return mappingSourceName == "SqlServerTypeMappingSource";
    }
    static ConversionResult QueryableToSql(object arg, IReadOnlyDictionary<string, object> context)
    {
        var queryable = (IQueryable) arg;

        var sql = queryable.ToQueryString();
        QueryableConverter.TryExecuteQueryable(queryable, out var result);
        return new(result, [new("sql", sql)]);
    }

    public static DbContextOptionsBuilder<TContext> ThrowForMissingOrderBy<TContext>(this DbContextOptionsBuilder<TContext> builder)
        where TContext : DbContext =>
        builder.ReplaceService<IShapedQueryCompilingExpressionVisitorFactory, RelationalFactory>();

    public static DbContextOptionsBuilder<TContext> EnableRecording<TContext>(this DbContextOptionsBuilder<TContext> builder)
        where TContext : DbContext
        => builder.EnableRecording(null);

    public static DbContextOptionsBuilder<TContext> EnableRecording<TContext>(this DbContextOptionsBuilder<TContext> builder, string? identifier)
        where TContext : DbContext =>
        builder.AddInterceptors(new LogCommandInterceptor(identifier));

    static ConcurrentBag<Guid> recordingDisabledContextIds = [];

    public static void DisableRecording<TContext>(this TContext context)
        where TContext : DbContext =>
        recordingDisabledContextIds.Add(context.ContextId.InstanceId);

    internal static bool IsRecordingDisabled<TContext>(this TContext context)
        where TContext : DbContext =>
        recordingDisabledContextIds.Contains(context.ContextId.InstanceId);
}