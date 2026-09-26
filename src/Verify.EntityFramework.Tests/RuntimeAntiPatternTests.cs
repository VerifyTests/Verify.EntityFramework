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

    [Test]
    public async Task SynchronousQuery()
    {
        await using var database = await DbContextBuilder.GetDatabase();
        await using var data = BuildSqlServer(database, _ => _.ThrowOnSynchronousCalls = true);

        // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
        await Throws(() => data.Companies.ToList())
            .IgnoreStackTrace();
        await data.Companies.ToListAsync();
    }

    [Test]
    public async Task SynchronousSaveChanges()
    {
        await using var data = BuildInMemory(_ => _.ThrowOnSynchronousCalls = true);
        data.Add(NewCompany(1));
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

    [Test]
    public async Task UnmodifiedTracking()
    {
        var name = nameof(UnmodifiedTracking);
        await Seed(name);

        var data = BuildInMemory(_ => _.ThrowOnUnmodifiedTracking = true, name);
        await data.Companies.ToListAsync();
        await Throws(() => data.Dispose())
            .IgnoreStackTrace();
    }

    [Test]
    public async Task NoTracking()
    {
        var name = nameof(NoTracking);
        await Seed(name);

        await using var data = BuildInMemory(_ => _.ThrowOnUnmodifiedTracking = true, name);
        await data.Companies
            .AsNoTracking()
            .ToListAsync();
    }

    [Test]
    public async Task TrackingThenSave()
    {
        var name = nameof(TrackingThenSave);
        await Seed(name);

        await using var data = BuildInMemory(_ => _.ThrowOnUnmodifiedTracking = true, name);
        var company = await data.Companies.FirstAsync();
        company.Name = "Renamed";
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
        Assert.Throws<Exception>(() => data.SaveChanges());
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
        builder.ThrowOnAntiPatterns(configure);
        builder.EnableServiceProviderCaching(false);
        return new(builder.Options);
    }

    static SampleDbContext BuildSqlServer(SqlDatabase<SampleDbContext> database, Action<AntiPatternOptions> configure)
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseSqlServer(database.Connection);
        builder.ThrowOnAntiPatterns(configure);
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
