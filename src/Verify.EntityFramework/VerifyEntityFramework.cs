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

            throw new("The `model` parameter must be provided wither on this method or on VerifyEntityFramework.Enable()");
        }

        return model.GetNavigations();
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

    public static void Initialize(DbContext context, bool recordCommands) =>
        Initialize(context.Model, recordCommands);

    public static bool Initialized { get; private set; }

    static bool recordCommands = true;

    public static void Initialize(IModel? model = null) =>
        Initialize(model, recordCommands: true);

    /// <param name="model">The <see cref="IModel" /> used to cache navigation property information. Can be null.</param>
    /// <param name="recordCommands">
    /// Allow <see cref="EnableRecording{TContext}(DbContextOptionsBuilder{TContext})" /> to add the interceptor that
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

    public static DbContextOptionsBuilder<TContext> EnableRecording<TContext>(this DbContextOptionsBuilder<TContext> builder)
        where TContext : DbContext
        => builder.EnableRecording(null);

    public static DbContextOptionsBuilder<TContext> EnableRecording<TContext>(this DbContextOptionsBuilder<TContext> builder, string? identifier)
        where TContext : DbContext
    {
        if (!recordCommands)
        {
            return builder;
        }

        var interceptor = new LogCommandInterceptor(identifier);
        ((IDbContextOptionsBuilderInfrastructure) builder).AddOrUpdateExtension(new RecordingOptionsExtension(interceptor));
        return builder.AddInterceptors(interceptor);
    }

    static ConcurrentBag<Guid> recordingDisabledContextIds = [];

    public static void DisableRecording<TContext>(this TContext context)
        where TContext : DbContext =>
        recordingDisabledContextIds.Add(context.ContextId.InstanceId);

    internal static bool IsRecordingDisabled<TContext>(this TContext context)
        where TContext : DbContext =>
        recordingDisabledContextIds.Contains(context.ContextId.InstanceId);
}
