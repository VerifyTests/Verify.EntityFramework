namespace VerifyTests;

public static class VerifyEntityFramework
{
    static List<(Type type, string name)>? modelNavigations;

    public static async IAsyncEnumerable<object> AllData(this DbContext data)
    {
        // A query for the root of a hierarchy already returns the derived entities
        foreach (var entityType in data
                     .Model
                     .GetEntityTypes()
                     .Where(_ => !_.IsOwned() &&
                                 _.BaseType == null)
                     .OrderBy(_ => _.Name))
        {
            var query = queryEntities.MakeGenericMethod(entityType.ClrType);
            var list = await (Task<List<object>>) query.Invoke(null, [data, entityType])!;
            foreach (var entity in list)
            {
                yield return entity;
            }
        }
    }

    static MethodInfo queryEntities = typeof(VerifyEntityFramework)
        .GetMethod(nameof(QueryEntities), BindingFlags.NonPublic | BindingFlags.Static)!;

    static MethodInfo efProperty = typeof(EF)
        .GetMethod(nameof(EF.Property))!;

    static async Task<List<object>> QueryEntities<T>(DbContext data, IEntityType entityType)
        where T : class
    {
        IQueryable<T> queryable;
        // A shared-type entity, for example an implicit many-to-many join entity, can only be accessed by name
        if (entityType.HasSharedClrType)
        {
            queryable = data.Set<T>(entityType.Name);
        }
        else
        {
            queryable = data.Set<T>();
        }

        queryable = queryable.AsNoTracking();

        var key = entityType.FindPrimaryKey();
        if (key != null)
        {
            var method = nameof(Queryable.OrderBy);
            foreach (var property in key.Properties)
            {
                var parameter = Expression.Parameter(typeof(T));
                var body = Expression.Call(
                    efProperty.MakeGenericMethod(property.ClrType),
                    parameter,
                    Expression.Constant(property.Name));
                var lambda = Expression.Lambda(body, parameter);
                queryable = queryable.Provider.CreateQuery<T>(
                    Expression.Call(
                        typeof(Queryable),
                        method,
                        [typeof(T), property.ClrType],
                        queryable.Expression,
                        Expression.Quote(lambda)));
                method = nameof(Queryable.ThenBy);
            }
        }

        var list = await queryable.ToListAsync();
        return [.. list];
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

    public static bool DisableSqlFormatting { get; set; }

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

            throw new("The `model` parameter must be provided either on this method or on VerifyEntityFramework.Initialize()");
        }

        return model.GetNavigations();
    }

    static IEnumerable<(Type type, string name)> GetNavigations(this IModel model)
    {
        foreach (var type in model.GetEntityTypes())
        {
            foreach (var navigation in type.GetNavigations())
            {
                // An owned type is part of its owner's data, like a complex property, so the navigation from the
                // owner to it is kept. The navigation from an owned type back to its owner is still ignored.
                if (navigation.ForeignKey.IsOwnership &&
                    !navigation.IsOnDependent)
                {
                    continue;
                }

                yield return new(type.ClrType, navigation.Name);
            }

            // many-to-many navigations are skip navigations, which GetNavigations does not include
            foreach (var navigation in type.GetSkipNavigations())
            {
                yield return new(type.ClrType, navigation.Name);
            }
        }
    }

    public static void Initialize(DbContext context) =>
        Initialize(context.Model);

    public static void Initialize(DbContext context, bool recordCommands) =>
        Initialize(context.Model, recordCommands);

    public static bool Initialized { get; private set; }

    static bool recordCommands = true;

    public static void Initialize(IModel? model = null) =>
        Initialize(model, recordCommands: true);

    /// <param name="model">The <see cref="IModel" /> used to cache navigation property information. Can be null.</param>
    /// <param name="recordCommands">
    /// Allow <see cref="EnableRecording{TContext}(DbContextOptionsBuilder{TContext}, string?, bool?)" /> to add the interceptor that
    /// adds executed commands to <see cref="Recording" /> under the name `ef`. Disable when another package, for
    /// example Verify.SqlServer, already records the same commands.
    /// </param>
    public static void Initialize(IModel? model, bool recordCommands)
    {
        if (Initialized)
        {
            throw new("Already Initialized");
        }

        Initialized = true;
        VerifyEntityFramework.recordCommands = recordCommands;

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
        VerifierSettings.IgnoreMembersWithType(typeof(IDbContextFactory<>));
        VerifierSettings.IgnoreMembersWithType<DbContext>();

        VerifierSettings.AddExtraSettings(settings =>
        {
            var converters = settings.Converters;
            converters.Add(new DbUpdateExceptionConverter());
            converters.Add(new TrackerConverter());
            converters.Add(new QueryableConverter());
            converters.Add(new LogEntryConverter());
            converters.Add(new SaveChangesEntryConverter());
        });
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
        if (queryable.ShouldFormatSql())
        {
            sql = SqlFormatter.Format(sql).ToString();
        }

        QueryableConverter.TryExecuteQueryable(queryable, out var result);
        return new(result, [new("sql", sql)]);
    }

    internal static bool ShouldFormatSql(this IQueryable queryable)
    {
        if (DisableSqlFormatting)
        {
            return false;
        }

        var model = FindModel(queryable.Expression);
        return model != null && model.IsSqlServer();
    }

    static IModel? FindModel(Expression? expression) =>
        expression switch
        {
            EntityQueryRootExpression root => root.EntityType.Model,
            MethodCallExpression { Arguments.Count: > 0 } method => FindModel(method.Arguments[0]),
            _ => null
        };

#pragma warning disable EF9002
    public static DbContextOptionsBuilder<TContext> UseDescriptiveTableAliases<TContext>(this DbContextOptionsBuilder<TContext> builder)
        where TContext : DbContext =>
        builder.ReplaceService<ISqlAliasManagerFactory, DescriptiveSqlAliasManagerFactory>();
#pragma warning restore EF9002

    public static DbContextOptionsBuilder<TContext> UseDescriptiveParameterNames<TContext>(this DbContextOptionsBuilder<TContext> builder)
        where TContext : DbContext =>
        builder
            .ReplaceService<IParameterNameGeneratorFactory, DescriptiveParameterFactory>()
            .ReplaceService<IModificationCommandFactory, DescriptiveParameterFactory>();

    /// <summary>
    /// The default for the `throwOnAntiPatterns` parameter of
    /// <see cref="EnableRecording{TContext}(DbContextOptionsBuilder{TContext}, string?, bool?)" />.
    /// </summary>
    public static bool ThrowOnAntiPatternsByDefault { get; set; } = true;

    /// <param name="identifier">Record under this identifier, so a test can start and stop its own recording.</param>
    /// <param name="throwOnAntiPatterns">
    /// Apply <see cref="ThrowOnAntiPatterns{TContext}" />. Defaults to <see cref="ThrowOnAntiPatternsByDefault" />.
    /// </param>
    public static DbContextOptionsBuilder<TContext> EnableRecording<TContext>(
        this DbContextOptionsBuilder<TContext> builder,
        string? identifier = null,
        bool? throwOnAntiPatterns = null)
        where TContext : DbContext
    {
        if (throwOnAntiPatterns ?? ThrowOnAntiPatternsByDefault)
        {
            builder.ThrowOnAntiPatterns();
        }

        if (!recordCommands)
        {
            return builder;
        }

        var interceptor = new LogCommandInterceptor(identifier);
        ((IDbContextOptionsBuilderInfrastructure) builder).AddOrUpdateExtension(new RecordingOptionsExtension(interceptor));
        return builder.AddInterceptors(interceptor);
    }

    /// <summary>
    /// Throw when a query that uses an anti-pattern is compiled. Detects:
    /// <list type="bullet">
    ///   <item>An Include, ThenInclude, or tracking option that EF ignores, since the query returns no entity. For example it ends in a projection, or a scalar like Count.</item>
    ///   <item>An ordering that EF discards, since it is followed by another OrderBy.</item>
    ///   <item>AsSplitQuery or AsSingleQuery on a query that loads no collection.</item>
    ///   <item>A redundant navigation null check, for example `_.Owner != null &amp;&amp; _.Owner.Name == "owner"`.</item>
    ///   <item>The query anti-patterns that EF detects but only logs, for example Take without OrderBy. To allow one, call ConfigureWarnings after this method.</item>
    /// </list>
    /// </summary>
    public static DbContextOptionsBuilder<TContext> ThrowOnAntiPatterns<TContext>(this DbContextOptionsBuilder<TContext> builder)
        where TContext : DbContext
    {
        ((IDbContextOptionsBuilderInfrastructure) builder).AddOrUpdateExtension(new AntiPatternOptionsExtension());
        return builder.ConfigureWarnings(_ => _.Throw(AntiPatternInterceptor.Warnings));
    }

    // Keyed on the whole ContextId, since a pooled context keeps its InstanceId and only increments its Lease.
    // A dictionary, since ConcurrentBag.Contains copies the bag, under a lock, for every command.
    static ConcurrentDictionary<DbContextId, byte> recordingDisabledContextIds = [];

    public static void DisableRecording<TContext>(this TContext context)
        where TContext : DbContext =>
        recordingDisabledContextIds.TryAdd(context.ContextId, 0);

    internal static bool IsRecordingDisabled<TContext>(this TContext context)
        where TContext : DbContext =>
        recordingDisabledContextIds.ContainsKey(context.ContextId);
}
