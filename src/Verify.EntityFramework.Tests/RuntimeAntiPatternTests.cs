[TestFixture]
[Parallelizable(ParallelScope.All)]
public class RuntimeAntiPatternTests
{
    // Opting in to some checks changes the service provider, and InMemory keeps a database per service provider
    static Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot databaseRoot = new();

    [Test]
    public async Task LazyLoading()
    {
        var name = nameof(LazyLoading);
        await using (var seed = BuildLazy(name, _ => { }))
        {
            seed.Add(
                new LazyBlog
                {
                    Id = 1,
                    Posts =
                    [
                        new()
                        {
                            Id = 1
                        }
                    ]
                });
            await seed.SaveChangesAsync();
        }

        await using var data = BuildLazy(name, _ => _.ThrowOnLazyLoading = true);
        var blog = await data.Blogs.SingleAsync();
        await Throws(() => blog.Posts)
            .IgnoreStackTrace();
    }

    [Test]
    public async Task LazyLoadingNotEnabled()
    {
        var name = nameof(LazyLoadingNotEnabled);
        await using (var seed = BuildLazy(name, _ => { }))
        {
            seed.Add(
                new LazyBlog
                {
                    Id = 1,
                    Posts =
                    [
                        new()
                        {
                            Id = 1
                        }
                    ]
                });
            await seed.SaveChangesAsync();
        }

        await using var data = BuildLazy(name, _ => { });
        var blog = await data.Blogs.SingleAsync();
        Assert.That(blog.Posts, Has.Count.EqualTo(1));
    }

    // lazy loading after the context is disposed, which is what happens when Verify serializes an entity after its
    // context is disposed. EF throws for it by default, with or without ThrowOnLazyLoading.
    [Test]
    public async Task LazyLoadingDisposedContext()
    {
        var name = nameof(LazyLoadingDisposedContext);
        await SeedLazy(name);

        LazyBlog blog;
        await using (var data = BuildLazy(name, _ => _.ThrowOnLazyLoading = true))
        {
            blog = await data.Blogs.SingleAsync();
        }

        await Throws(() => blog.Posts)
            .IgnoreStackTrace();
    }

    static async Task SeedLazy(string name)
    {
        await using var seed = BuildLazy(name, _ => { });
        seed.Add(
            new LazyBlog
            {
                Id = 1,
                Posts =
                [
                    new()
                    {
                        Id = 1
                    }
                ]
            });
        await seed.SaveChangesAsync();
    }

    [Test]
    public async Task SynchronousQuery()
    {
        await using var database = await DbContextBuilder.GetDatabase();
        await using var data = BuildSqlServer(database, _ => _.ThrowOnSynchronousCalls = true);

        // ReSharper disable once ReturnValueOfPureMethodIsNotUsed, MethodHasAsyncOverload
        await Throws(() => data.Companies.ToList())
            .IgnoreStackTrace();
        await data.Companies.ToListAsync();
    }

    [Test]
    public async Task SynchronousSaveChanges()
    {
        await using var data = BuildInMemory(_ => _.ThrowOnSynchronousCalls = true);
        data.Add(NewCompany(1));
        // ReSharper disable once MethodHasAsyncOverload
        await Throws(() => data.SaveChanges())
            .IgnoreStackTrace();
        await data.SaveChangesAsync();
    }

    [Test]
    public async Task RepeatedQueries()
    {
        await using var database = await DbContextBuilder.GetDatabase();
        await using var data = BuildSqlServer(
            database,
            _ =>
            {
                _.ThrowOnRepeatedQueries = true;
                _.RepeatedQueryThreshold = 2;
            });

        #region RepeatedQueries

        await ThrowsTask(async () =>
            {
                foreach (var id in new[] { 1, 4, 6 })
                {
                    await data.Companies
                        .Where(_ => _.Id == id)
                        .ToListAsync();
                }
            })
            .IgnoreStackTrace();

        #endregion
    }

    [Test]
    public async Task RepeatedQueriesUnderThreshold()
    {
        await using var database = await DbContextBuilder.GetDatabase();
        await using var data = BuildSqlServer(
            database,
            _ =>
            {
                _.ThrowOnRepeatedQueries = true;
                _.RepeatedQueryThreshold = 2;
            });

        foreach (var id in new[] { 1, 4 })
        {
            await data.Companies
                .Where(_ => _.Id == id)
                .ToListAsync();
        }
    }

    [Test]
    public async Task RepeatedSaveChanges()
    {
        await using var data = BuildInMemory(
            _ =>
            {
                _.ThrowOnRepeatedSaveChanges = true;
                _.RepeatedSaveChangesThreshold = 2;
            });

        await ThrowsTask(async () =>
            {
                for (var id = 1; id <= 3; id++)
                {
                    data.Add(NewCompany(id));
                    data.Add(NewCompany(id + 100));
                    await data.SaveChangesAsync();
                }
            })
            .IgnoreStackTrace();
    }

    // a SaveChanges with nothing to save is not counted
    [Test]
    public async Task RepeatedSaveChangesWithoutChanges()
    {
        await using var data = BuildInMemory(
            _ =>
            {
                _.ThrowOnRepeatedSaveChanges = true;
                _.RepeatedSaveChangesThreshold = 2;
            });

        for (var i = 0; i < 3; i++)
        {
            await data.SaveChangesAsync();
        }
    }

    [Test]
    public async Task SingleRowSaves()
    {
        await using var data = BuildInMemory(
            _ =>
            {
                _.ThrowOnSingleRowSaves = true;
                _.SingleRowSavesThreshold = 2;
            });

        #region SingleRowSaves

        await ThrowsTask(async () =>
            {
                for (var id = 1; id <= 3; id++)
                {
                    data.Add(NewCompany(id));
                    await data.SaveChangesAsync();
                }
            })
            .IgnoreStackTrace();

        #endregion
    }

    [Test]
    public async Task MultiRowSaves()
    {
        await using var data = BuildInMemory(
            _ =>
            {
                _.ThrowOnSingleRowSaves = true;
                _.SingleRowSavesThreshold = 2;
            });

        for (var id = 1; id <= 3; id++)
        {
            data.Add(NewCompany(id));
            data.Add(NewCompany(id + 100));
            await data.SaveChangesAsync();
        }
    }

    [Test]
    public async Task LoadThenDelete()
    {
        await using var data = await BuildWithCompanies(3);

        #region LoadThenDelete

        var companies = await data.Companies.ToListAsync();
        data.RemoveRange(companies);
        await ThrowsTask(() => data.SaveChangesAsync())
            .IgnoreStackTrace();

        #endregion
    }

    [Test]
    public async Task LoadThenUpdate()
    {
        await using var data = await BuildWithCompanies(3);

        var companies = await data.Companies.ToListAsync();
        foreach (var company in companies)
        {
            company.Name = "Renamed";
        }

        await ThrowsTask(() => data.SaveChangesAsync())
            .IgnoreStackTrace();
    }

    // each entity gets a different change, which ExecuteUpdate can not do in one statement
    [Test]
    public async Task LoadThenDifferentChanges()
    {
        await using var data = await BuildWithCompanies(3);

        var companies = await data.Companies.ToListAsync();
        companies[0].Name = "Renamed";
        data.Remove(companies[1]);
        data.Remove(companies[2]);
        await data.SaveChangesAsync();
    }

    // every check is off unless opted in to
    [Test]
    public async Task NotEnabled()
    {
        await using var data = BuildInMemory(_ => { });
        for (var id = 1; id <= 20; id++)
        {
            data.Add(NewCompany(id));
            // ReSharper disable once MethodHasAsyncOverload
            data.SaveChanges();
        }

        var companies = await data.Companies.ToListAsync();
        data.RemoveRange(companies);
        await data.SaveChangesAsync();
    }

    // the options from an earlier call are kept, so the order of EnableRecording does not matter
    [Test]
    public async Task OptionsKeptByEnableRecording()
    {
        #region ThrowOnAntiPatternsRuntime

        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(nameof(OptionsKeptByEnableRecording));
        builder.EnableRecording();
        builder.ThrowOnAntiPatterns(
            _ =>
            {
                _.ThrowOnSynchronousCalls = true;
                _.ThrowOnSingleRowSaves = true;
                _.SingleRowSavesThreshold = 5;
            });

        #endregion

        builder.EnableRecording();
        builder.EnableServiceProviderCaching(false);
        await using var data = new SampleDbContext(builder.Options);
        data.Add(NewCompany(1));
        Recording.Start();
        // ReSharper disable once MethodHasAsyncOverload
        Assert.Throws<Exception>(() => data.SaveChanges());
        Recording.Stop();
    }

    // the setup of a test is not counted, only what runs while Verify is recording
    [Test]
    public async Task OnlyWhileRecording()
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(nameof(OnlyWhileRecording), databaseRoot);
        builder.ThrowOnAntiPatterns(_ => _.ThrowOnSingleRowSaves = true);
        builder.EnableServiceProviderCaching(false);
        await using var data = new SampleDbContext(builder.Options);

        #region AntiPatternsOnlyWhileRecording

        // setup, which is not counted
        for (var id = 1; id <= 3; id++)
        {
            data.Add(NewCompany(id));
            await data.SaveChangesAsync();
        }

        Recording.Start();

        // the code under test
        Assert.ThrowsAsync<Exception>(async () =>
        {
            for (var id = 11; id <= 13; id++)
            {
                data.Add(NewCompany(id));
                await data.SaveChangesAsync();
            }
        });

        Recording.Stop();

        #endregion
    }

    [Test]
    public async Task NotRecording()
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(nameof(NotRecording), databaseRoot);
        builder.ThrowOnAntiPatterns(_ => _.ThrowOnSynchronousCalls = true);
        builder.EnableServiceProviderCaching(false);
        await using var data = new SampleDbContext(builder.Options);

        data.Add(NewCompany(1));
        // ReSharper disable once MethodHasAsyncOverload
        data.SaveChanges();
    }

    // EnableRecording with an identifier only checks while that recording is started
    [Test]
    public async Task RecordingIdentifier()
    {
        var identifier = nameof(RuntimeAntiPatternTests) + nameof(RecordingIdentifier);
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(nameof(RecordingIdentifier), databaseRoot);
        builder.EnableRecording(identifier);
        builder.ThrowOnAntiPatterns(_ => _.ThrowOnSynchronousCalls = true);
        builder.EnableServiceProviderCaching(false);
        await using var data = new SampleDbContext(builder.Options);

        Recording.Start();
        data.Add(NewCompany(1));
        // ReSharper disable once MethodHasAsyncOverload
        data.SaveChanges();
        Recording.Stop();

        Recording.Start(identifier);
        data.Add(NewCompany(2));
        // ReSharper disable once MethodHasAsyncOverload
        Assert.Throws<Exception>(() => data.SaveChanges());
        Recording.Stop(identifier);
    }

    static Company NewCompany(int id) =>
        new()
        {
            Id = id,
            Name = $"Company{id}"
        };

    static async Task Seed(string name)
    {
        await using var seed = BuildInMemory(_ => { }, name);
        seed.Add(NewCompany(1));
        seed.Add(NewCompany(2));
        await seed.SaveChangesAsync();
    }

    static async Task<SampleDbContext> BuildWithCompanies(int count, [CallerMemberName] string name = "")
    {
        await using (var seed = BuildInMemory(_ => { }, name))
        {
            for (var id = 1; id <= count; id++)
            {
                seed.Add(NewCompany(id));
            }

            await seed.SaveChangesAsync();
        }

        return BuildInMemory(
            _ =>
            {
                _.ThrowOnLoadThenModify = true;
                _.LoadThenModifyThreshold = 2;
            },
            name);
    }

    static SampleDbContext BuildInMemory(Action<AntiPatternOptions> configure, [CallerMemberName] string name = "")
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(nameof(RuntimeAntiPatternTests) + name, databaseRoot);
        builder.ThrowOnAntiPatterns(_ =>
        {
            // these tests check each check's logic, so whether Verify is recording is tested separately
            _.OnlyWhileRecording = false;
            configure(_);
        });
        builder.EnableServiceProviderCaching(false);
        return new(builder.Options);
    }

    static SampleDbContext BuildSqlServer(SqlDatabase<SampleDbContext> database, Action<AntiPatternOptions> configure)
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseSqlServer(database.Connection);
        builder.ThrowOnAntiPatterns(_ =>
        {
            _.OnlyWhileRecording = false;
            configure(_);
        });
        builder.EnableServiceProviderCaching(false);
        return new(builder.Options);
    }

    static LazyContext BuildLazy(string name, Action<AntiPatternOptions> configure)
    {
        var builder = new DbContextOptionsBuilder<LazyContext>();
        builder.UseInMemoryDatabase(nameof(RuntimeAntiPatternTests) + name, databaseRoot);
        builder.ThrowOnAntiPatterns(configure);
        builder.EnableServiceProviderCaching(false);
        return new(builder.Options);
    }

    public class LazyContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<LazyBlog> Blogs { get; set; } = null!;
    }

    public class LazyBlog
    {
        ILazyLoader? loader;
        List<LazyPost> posts = [];

        public LazyBlog()
        {
        }

        // EF passes the lazy loader to this constructor
        // ReSharper disable once UnusedMember.Local
        LazyBlog(ILazyLoader loader) =>
            this.loader = loader;

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public List<LazyPost> Posts
        {
            get => loader.Load(this, ref posts!)!;
            set => posts = value;
        }
    }

    public class LazyPost
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
    }
}
