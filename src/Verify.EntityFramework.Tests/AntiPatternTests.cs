[TestFixture]
[Parallelizable(ParallelScope.All)]
public class AntiPatternTests
{
    // ReSharper disable once UnusedVariable
    static void Build(string databaseName)
    {
        #region ThrowOnAntiPatterns

        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseSqlServer(connectionString);
        builder.ThrowOnAntiPatterns();
        var data = new SampleDbContext(builder.Options);

        #endregion
    }

    // ToQueryString does not connect
    static string connectionString = "Server=.;Database=AntiPatterns;Trusted_Connection=True";

    [Test]
    public async Task IncludeThenProjection()
    {
        await using var data = BuildData();

        #region IgnoredInclude

        await ThrowsTask(() =>
                data.Companies
                    .Include(_ => _.Employees)
                    .Select(_ => new
                    {
                        _.Name,
                        EmployeeCount = _.Employees.Count
                    })
                    .ToListAsync())
            .IgnoreStackTrace();

        #endregion
    }

    [Test]
    public async Task IncludeThenScalarProjection()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Companies
                    .Include(_ => _.Employees)
                    .Select(_ => _.Name)
                    .ToListAsync())
            .IgnoreStackTrace();
    }

    [Test]
    public async Task IncludeThenCount()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Companies
                    .Include(_ => _.Employees)
                    .CountAsync())
            .IgnoreStackTrace();
    }

    [Test]
    public async Task IncludeThenAny()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Companies
                    .Include(_ => _.Employees)
                    .AnyAsync(_ => _.Name == "Title"))
            .IgnoreStackTrace();
    }

    [Test]
    public async Task ThenIncludeThenProjection()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Employees
                    .Include(_ => _.Company)
                    .ThenInclude(_ => _.Employees)
                    .Where(_ => _.Age > 30)
                    .OrderBy(_ => _.Name)
                    .Select(_ => new
                    {
                        _.Name,
                        Company = _.Company.Name
                    })
                    .ToListAsync())
            .IgnoreStackTrace();
    }

    [Test]
    public async Task StringInclude()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Companies
                    .Include("Employees")
                    .Select(_ => _.Id)
                    .ToListAsync())
            .IgnoreStackTrace();
    }

    [Test]
    public async Task IncludeThenGroupByKey()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Employees
                    .Include(_ => _.Company)
                    .GroupBy(_ => _.CompanyId)
                    .Select(_ => _.Key)
                    .ToListAsync())
            .IgnoreStackTrace();
    }

    // an entity kept by a projection is lost by a later one
    [Test]
    public async Task IncludeThenEntityThenScalar()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Companies
                    .Include(_ => _.Employees)
                    .Select(_ => new
                    {
                        Company = _,
                        _.Name
                    })
                    .Where(_ => _.Name != "")
                    .Select(_ => _.Company.Id)
                    .ToListAsync())
            .IgnoreStackTrace();
    }

    // the query is checked by any provider, including when converted to SQL, as done when verifying a queryable
    [Test]
    public void SqlServerToQueryString()
    {
        using var data = BuildSqlServerData();

        var query = data.Companies
            .Include(_ => _.Employees)
            .Select(_ => _.Name);
        var exception = Assert.Throws<Exception>(() => query.ToQueryString());
        Assert.That(exception!.Message, Does.StartWith("Include(_ => _.Employees) is ignored"));
    }

    // a query that throws is not cached, so it throws every time
    [Test]
    public void ThrowsOnEachExecution()
    {
        using var data = BuildData();
        for (var i = 0; i < 2; i++)
        {
            Assert.ThrowsAsync<Exception>(() =>
                data.Companies
                    .Include(_ => _.Employees)
                    .Select(_ => _.Name)
                    .ToListAsync());
        }
    }

    [Test]
    public async Task IncludeEntities()
    {
        await using var data = BuildData();
        await data.Companies
            .Include(_ => _.Employees)
            .Where(_ => _.Name != "")
            .OrderBy(_ => _.Name)
            .ToListAsync();
        await data.Companies
            .Include(_ => _.Employees)
            .OrderBy(_ => _.Id)
            .FirstOrDefaultAsync();
    }

    [Test]
    public async Task IncludeThenProjectionOfEntity()
    {
        await using var data = BuildData();
        await data.Companies
            .Include(_ => _.Employees)
            .Select(_ => new
            {
                Company = _,
                _.Name
            })
            .Where(_ => _.Name != "")
            .ToListAsync();
        await data.Companies
            .Include(_ => _.Employees)
            .Select(_ => new
            {
                _.Name,
                Employees = _.Employees.ToList()
            })
            .ToListAsync();
        await data.Companies
            .Include(_ => _.Employees)
            .Select(_ => (object) _)
            .ToListAsync();
    }

    [Test]
    public async Task IncludeThenGroupByEntities()
    {
        await using var data = BuildData();
        await data.Employees
            .Include(_ => _.Company)
            .GroupBy(_ => _.CompanyId)
            .Select(_ => new
            {
                _.Key,
                Employees = _.ToList()
            })
            .ToListAsync();
    }

    [Test]
    public async Task ProjectionWithoutInclude()
    {
        await using var data = BuildData();
        await data.Companies
            .Select(_ => new
            {
                _.Name,
                EmployeeCount = _.Employees.Count
            })
            .ToListAsync();
        await data.Companies.CountAsync();
    }

    [Test]
    public async Task NotEnabled()
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(nameof(NotEnabled));
        await using var data = new SampleDbContext(builder.Options);

        await data.Companies
            .Include(_ => _.Employees)
            .Select(_ => _.Name)
            .ToListAsync();
    }

    [Test]
    public async Task AsNoTrackingThenProjection()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Companies
                    .AsNoTracking()
                    .Select(_ => _.Name)
                    .ToListAsync())
            .IgnoreStackTrace();
    }

    [Test]
    public async Task ProjectionThenAsNoTracking()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Companies
                    .Select(_ => new
                    {
                        _.Name
                    })
                    .AsNoTracking()
                    .ToListAsync())
            .IgnoreStackTrace();
    }

    [Test]
    public async Task IncludeAndAsNoTrackingThenProjection()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Companies
                    .Include(_ => _.Employees)
                    .AsNoTracking()
                    .Select(_ => _.Name)
                    .ToListAsync())
            .IgnoreStackTrace();
    }

    [Test]
    public async Task AsTrackingThenCount()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Companies
                    .AsTracking()
                    .CountAsync())
            .IgnoreStackTrace();
    }

    [Test]
    public async Task AsNoTrackingEntities()
    {
        await using var data = BuildData();
        await data.Companies
            .AsNoTracking()
            .Where(_ => _.Name != "")
            .ToListAsync();
        await data.Companies
            .AsNoTrackingWithIdentityResolution()
            .Select(_ => new
            {
                Company = _,
                _.Name
            })
            .ToListAsync();
    }

    [Test]
    public async Task OrderByThenOrderBy()
    {
        await using var data = BuildData();

        #region DiscardedOrderBy

        await ThrowsTask(() =>
                data.Companies
                    .OrderBy(_ => _.Name)
                    .OrderBy(_ => _.Id)
                    .ToListAsync())
            .IgnoreStackTrace();

        #endregion
    }

    [Test]
    public async Task OrderByDescendingThenOrderByDescending()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Companies
                    .OrderByDescending(_ => _.Name)
                    .OrderByDescending(_ => _.Id)
                    .ToListAsync())
            .IgnoreStackTrace();
    }

    [Test]
    public async Task ThenByThenOrderBy()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Companies
                    .OrderBy(_ => _.Name)
                    .ThenBy(_ => _.Id)
                    .Where(_ => _.Name != "")
                    .OrderBy(_ => _.Id)
                    .ToListAsync())
            .IgnoreStackTrace();
    }

    [Test]
    public async Task OrderByThenOrderByInProjection()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Companies
                    .Select(_ => new
                    {
                        _.Name,
                        Employees = _.Employees
                            .OrderBy(_ => _.Name)
                            .OrderBy(_ => _.Age)
                            .Select(_ => _.Name)
                            .ToList()
                    })
                    .ToListAsync())
            .IgnoreStackTrace();
    }

    [Test]
    public async Task OrderingKept()
    {
        await using var data = BuildData();
        await data.Companies
            .OrderBy(_ => _.Name)
            .ThenByDescending(_ => _.Id)
            .ToListAsync();

        // the second OrderBy orders the page selected by the first
        await data.Companies
            .OrderBy(_ => _.Name)
            .Take(10)
            .OrderBy(_ => _.Id)
            .ToListAsync();
    }

    // an anti-pattern that EF detects, but only logs. The relational pipeline logs it, so InMemory does not.
    [Test]
    public async Task TakeWithoutOrderBy()
    {
        await using var data = BuildSqlServerData();
        await Throws(() =>
                data.Companies
                    .Take(10)
                    .ToQueryString())
            .IgnoreStackTrace();
    }

    // the Company of each Employee is already populated by fix-up
    [Test]
    public async Task IncludeInverseNavigation()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Companies
                    .Include(_ => _.Employees)
                    .ThenInclude(_ => _.Company)
                    .ToListAsync())
            .IgnoreStackTrace();
    }

    [Test]
    public async Task AllowWarning()
    {
        #region AllowAntiPatternWarning

        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseSqlServer(connectionString);
        builder.ThrowOnAntiPatterns();
        builder.ConfigureWarnings(_ =>
            _.Ignore(CoreEventId.RowLimitingOperationWithoutOrderByWarning));

        #endregion

        await using var data = new SampleDbContext(builder.Options);
        data.Companies
            .Take(10)
            .ToQueryString();
    }

    [Test]
    public void EnabledByEnableRecording()
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(nameof(EnabledByEnableRecording));
        builder.EnableRecording();
        using var data = new SampleDbContext(builder.Options);

        Assert.ThrowsAsync<Exception>(() =>
            data.Companies
                .Include(_ => _.Employees)
                .Select(_ => _.Name)
                .ToListAsync());
    }

    [Test]
    public async Task EnableRecordingOptOut()
    {
        #region EnableRecordingAllowAntiPatterns

        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(nameof(EnableRecordingOptOut));
        builder.EnableRecording(throwOnAntiPatterns: false);

        #endregion

        await using var data = new SampleDbContext(builder.Options);
        await data.Companies
            .Include(_ => _.Employees)
            .Select(_ => _.Name)
            .ToListAsync();
    }

    [Test]
    public async Task SplitQueryWithoutCollection()
    {
        await using var data = BuildSqlServerData();

        #region IgnoredSplitQuery

        await Throws(() =>
                data.Employees
                    .Include(_ => _.Company)
                    .AsSplitQuery()
                    .ToQueryString())
            .IgnoreStackTrace();

        #endregion
    }

    [Test]
    public async Task SingleQueryWithoutCollection()
    {
        await using var data = BuildSqlServerData();
        await Throws(() =>
                data.Companies
                    .AsSingleQuery()
                    .Where(_ => _.Name != "")
                    .ToQueryString())
            .IgnoreStackTrace();
    }

    [Test]
    public void SplitQueryKept()
    {
        using var data = BuildSqlServerData();

        // a single collection is split from its parent
        data.Companies
            .Include(_ => _.Employees)
            .AsSplitQuery()
            .ToQueryString();

        data.Employees
            .Include(_ => _.Company)
            .ThenInclude(_ => _.Employees)
            .AsSplitQuery()
            .ToQueryString();

        data.Companies
            .AsSplitQuery()
            .Select(_ => new
            {
                _.Name,
                Employees = _.Employees.ToList()
            })
            .ToQueryString();

        data.Companies
            .Include("Employees")
            .AsSplitQuery()
            .ToQueryString();
    }

    static SampleDbContext BuildSqlServerData()
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseSqlServer(connectionString);
        builder.ThrowOnAntiPatterns();
        return new(builder.Options);
    }

    static SampleDbContext BuildData([CallerMemberName] string databaseName = "")
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(databaseName);
        builder.ThrowOnAntiPatterns();
        return new(builder.Options);
    }
}
