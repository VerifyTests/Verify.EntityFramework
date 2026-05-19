public class SqlInstanceProvider<TDbContext> : IAsyncDisposable
    where TDbContext : DbContext
{
    readonly TemplateFromContext<TDbContext>? buildTemplate;
    readonly ConstructInstance<TDbContext> constructInstance;

    SemaphoreSlim semaphore = new(1);
    SqlInstance<TDbContext>? sqlInstance;
    MsSqlContainer? sqlContainer;

    public SqlInstanceProvider(ConstructInstance<TDbContext> constructInstance, TemplateFromContext<TDbContext>? buildTemplate = null, string? storageSuffix = null)
    {
        this.buildTemplate = buildTemplate;
        this.constructInstance = constructInstance;
        if (string.Equals(Environment.GetEnvironmentVariable("VERIFY_ENTITYFRAMEWORK_TESTS_SQLENGINE"), "Docker", StringComparison.OrdinalIgnoreCase) || !OperatingSystem.IsWindows())
        {
            var suffix = storageSuffix == null ? "" : $".{storageSuffix}";
            sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest")
                .WithName($"Verify.EntityFramework.Tests{suffix}")
                .WithReuse(true)
                .Build();
        }
        else
        {
            sqlInstance = new(
                buildTemplate: buildTemplate,
                storage: storageSuffix == null ? null : Storage.FromSuffix<TDbContext>(storageSuffix),
                constructInstance: constructInstance);
        }
    }

    public async Task<ISqlDatabase<TDbContext>> Build(IEnumerable<object> data, [CallerFilePath] string testFile = "", string? databaseSuffix = null, [CallerMemberName] string memberName = "")
    {
        if (sqlInstance != null)
        {
            var sqlDatabase = await sqlInstance.Build(data, testFile, databaseSuffix, memberName);
            return new LocalDbSqlDatabase<TDbContext>(sqlDatabase);
        }

        if (sqlContainer != null)
        {
            var testClass = Path.GetFileNameWithoutExtension(testFile);
            var dbName = databaseSuffix == null ? $"{testClass}_{memberName}" : $"{testClass}_{memberName}_{databaseSuffix}";
            var sqlDatabase = await Build(dbName);
            await sqlDatabase.AddData(data);
            return sqlDatabase;
        }

        throw new UnreachableException("Both sqlInstance and sqlContainer can't be null at the same time");
    }

    public async Task<ISqlDatabase<TDbContext>> Build([CallerMemberName] string dbName = "")
    {
        if (sqlInstance != null)
        {
            var sqlDatabase = await sqlInstance.Build(dbName);
            return new LocalDbSqlDatabase<TDbContext>(sqlDatabase);
        }

        if (sqlContainer != null)
        {
            await semaphore.WaitAsync();
            try
            {
                if (sqlContainer.State != TestcontainersStates.Running)
                {
                    await sqlContainer.StartAsync();
                }
            }
            finally
            {
                semaphore.Release();
            }

            var result = await sqlContainer.ExecScriptAsync($"DROP DATABASE IF EXISTS [{dbName}]");
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to drop database '{dbName}' ({result.ExitCode})\n{result.Stdout}\n{result.Stderr}");
            }

            await using var dbContext = CreateDbContext(dbName);
            await dbContext.Database.EnsureCreatedAsync();

            if (buildTemplate != null)
            {
                await buildTemplate(dbContext);
            }

            return new ContainerSqlDatabase<TDbContext>(() => CreateDbContext(dbName));
        }

        throw new UnreachableException("Both sqlInstance and sqlContainer can't be null at the same time");
    }

    private TDbContext CreateDbContext(string dbName)
    {
        var connectionString = new SqlConnectionStringBuilder(sqlContainer!.GetConnectionString()) { InitialCatalog = dbName }.ConnectionString;
        return constructInstance(new DbContextOptionsBuilder<TDbContext>().UseSqlServer(connectionString));
    }

    public ValueTask DisposeAsync()
    {
        semaphore.Dispose();
        sqlInstance?.Dispose();
        return sqlContainer?.DisposeAsync() ?? ValueTask.CompletedTask;
    }
}