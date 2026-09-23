using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class InMemoryRecordingTests
{
    // ReSharper disable once UnusedVariable
    static void Build(string databaseName)
    {
        #region EnableRecordingInMemory

        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(databaseName);
        builder.EnableRecording();
        var data = new SampleDbContext(builder.Options);

        #endregion
    }

    [Test]
    public async Task RecordingInMemory()
    {
        await using var data = BuildData();

        #region RecordingInMemory

        Recording.Start();

        data.Add(
            new Company
            {
                Id = 1,
                Name = "Title"
            });
        await data.SaveChangesAsync();

        await data
            .Companies
            .Where(_ => _.Name == "Title")
            .ToListAsync();

        await Verify();

        #endregion
    }

    [Test]
    public async Task Parameters()
    {
        await using var data = BuildData();

        Recording.Start();

        var name = "Title";
        var ids = new[] { 1, 2, 4, 6 };
        await data
            .Companies
            .Where(_ => _.Name == name && ids.Contains(_.Id))
            .ToListAsync();

        await Verify();
    }

    [Test]
    public async Task Include()
    {
        await using var data = BuildData();

        Recording.Start();

        await data
            .Companies
            .Include(_ => _.Employees)
            .OrderBy(_ => _.Name)
            .ThenBy(_ => _.Id)
            .ToListAsync();

        await Verify();
    }

    [Test]
    public async Task Scalar()
    {
        await using var data = BuildData();

        Recording.Start();

        await data.Companies.CountAsync();
        await data.Companies.AnyAsync(_ => _.Name == "Title");

        await Verify();
    }

    [Test]
    public async Task Sync()
    {
        await using var data = BuildData();

        Recording.Start();

        data.Add(
            new Company
            {
                Id = 1,
                Name = "Title"
            });
        data.SaveChanges();

        data
            .Companies
            .Where(_ => _.Name == "Title")
            .ToList();
        data.Companies.Count();

        await Verify();
    }

    [Test]
    public async Task Modified()
    {
        await using var data = BuildData();
        data.Add(
            new Company
            {
                Id = 1,
                Name = "Before"
            });
        await data.SaveChangesAsync();

        Recording.Start();

        var company = await data.Companies.SingleAsync();
        company.Name = "After";
        await data.SaveChangesAsync();

        await Verify();
    }

    [Test]
    public async Task Deleted()
    {
        await using var data = BuildData();
        var company = new Company
        {
            Id = 1,
            Name = "Company"
        };
        data.Add(company);
        data.Add(
            new Employee
            {
                Id = 2,
                Name = "Employee",
                Company = company
            });
        await data.SaveChangesAsync();

        Recording.Start();

        // cascades to the employee
        data.Remove(company);
        await data.SaveChangesAsync();

        await Verify();
    }

    // the interceptor runs after the one added by EnableRecording
    [Test]
    public async Task ChangedByLaterInterceptor()
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(nameof(ChangedByLaterInterceptor));
        builder.EnableRecording();
        builder.AddInterceptors(new UpperCaseNameInterceptor());
        await using var data = new SampleDbContext(builder.Options);

        Recording.Start();

        data.Add(
            new Company
            {
                Id = 1,
                Name = "Title"
            });
        await data.SaveChangesAsync();

        await Verify();
    }

    class UpperCaseNameInterceptor :
        SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, Cancel cancel = default)
        {
            foreach (var entry in data.Context!.ChangeTracker.Entries<Company>())
            {
                entry.Entity.Name = entry.Entity.Name.ToUpperInvariant();
            }

            return new(result);
        }
    }

    [Test]
    public async Task NoChanges()
    {
        await using var data = BuildData();

        Recording.Start();

        await data.SaveChangesAsync();

        Assert.That(Recording.Stop(), Is.Empty);
    }

    [Test]
    public async Task DisableRecording()
    {
        await using var data = BuildData();

        Recording.Start();

        await data
            .Companies
            .Where(_ => _.Name == "Title")
            .ToListAsync();
        data.DisableRecording();
        await data
            .Companies
            .Where(_ => _.Name == "Disabled")
            .ToListAsync();
        data.Add(
            new Company
            {
                Id = 1,
                Name = "Disabled"
            });
        await data.SaveChangesAsync();

        await Verify();
    }

    [Test]
    public async Task Identifier()
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(nameof(Identifier));
        builder.EnableRecording(nameof(InMemoryRecordingTests) + nameof(Identifier));
        await using var data = new SampleDbContext(builder.Options);

        Recording.Start(nameof(InMemoryRecordingTests) + nameof(Identifier));

        data.Add(
            new Company
            {
                Id = 1,
                Name = "Title"
            });
        await data.SaveChangesAsync();
        await data
            .Companies
            .Where(_ => _.Name == "Title")
            .ToListAsync();

        var entries = Recording.Stop(nameof(InMemoryRecordingTests) + nameof(Identifier));
        await Verify(entries);
    }

    [Test]
    public async Task MultiDbContexts()
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(nameof(MultiDbContexts));
        builder.EnableRecording();

        await using var data1 = new SampleDbContext(builder.Options);
        Recording.Start();
        data1.Add(
            new Company
            {
                Id = 1,
                Name = "Title"
            });
        await data1.SaveChangesAsync();

        await using var data2 = new SampleDbContext(builder.Options);
        await data2
            .Companies
            .Where(_ => _.Name == "Title")
            .ToListAsync();

        await Verify();
    }

    // the query compiler is registered before the provider adds its services
    [Test]
    public async Task EnableRecordingBeforeProvider()
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.EnableRecording();
        builder.UseInMemoryDatabase(nameof(EnableRecordingBeforeProvider));
        await using var data = new SampleDbContext(builder.Options);

        Recording.Start();

        await data
            .Companies
            .Where(_ => _.Name == "Title")
            .ToListAsync();

        await Verify();
    }

    // EF does not apply extension services to an internal service provider, so nothing is recorded,
    // but it also rejects ReplaceService there, which EnableRecording must not use
    [Test]
    public async Task InternalServiceProvider()
    {
        var provider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(nameof(InternalServiceProvider));
        builder.UseInternalServiceProvider(provider);
        builder.EnableRecording();
        await using var data = new SampleDbContext(builder.Options);

        await data.Companies.ToListAsync();
    }

    // at the cost of the InMemory queries not being recorded
    [Test]
    public void KeepsReplacedQueryCompiler()
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(nameof(KeepsReplacedQueryCompiler));
        ((IDbContextOptionsBuilderInfrastructure) builder).AddOrUpdateExtension(new QueryCompilerExtension());
        builder.EnableRecording();
        using var data = new SampleDbContext(builder.Options);

        Assert.That(data.GetService<IQueryCompiler>(), Is.TypeOf<QueryCompiler>());
    }

    // mimics a library, for example EntityFrameworkCore.Projectables, that replaces IQueryCompiler
    class QueryCompilerExtension :
        IDbContextOptionsExtension
    {
        public DbContextOptionsExtensionInfo Info => field ??= new ExtensionInfo(this);

        public void ApplyServices(IServiceCollection services) =>
            services.Replace(ServiceDescriptor.Scoped<IQueryCompiler>(_ => ActivatorUtilities.CreateInstance<QueryCompiler>(_)));

        public void Validate(IDbContextOptions options)
        {
        }

        class ExtensionInfo(IDbContextOptionsExtension extension) :
            DbContextOptionsExtensionInfo(extension)
        {
            public override bool IsDatabaseProvider => false;

            public override string LogFragment => "";

            public override int GetServiceProviderHashCode() => 0;

            public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) =>
                other is ExtensionInfo;

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
            }
        }
    }

    static SampleDbContext BuildData([CallerMemberName] string databaseName = "")
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(databaseName);
        builder.EnableRecording();
        return new(builder.Options);
    }
}
