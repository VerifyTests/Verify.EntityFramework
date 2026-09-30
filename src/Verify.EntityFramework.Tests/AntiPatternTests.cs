public class AntiPatternTests
{
    // ReSharper disable once UnusedVariable
    // ReSharper disable once UnusedParameter.Local
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
    public async Task IncludeThenGroupByCount()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Employees
                    .Include(_ => _.Company)
                    .GroupBy(_ => _.CompanyId)
                    .Select(_ => _.Count())
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
    public async Task SqlServerToQueryString()
    {
        using var data = BuildSqlServerData();

        var query = data.Companies
            .Include(_ => _.Employees)
            .Select(_ => _.Name);
        var exception = Assert.ThrowsExactly<Exception>(() => query.ToQueryString());
        await Assert.That(exception!.Message).StartsWith("Include(_ => _.Employees) is ignored");
    }

    // a query that throws is not cached, so it throws every time
    [Test]
    public async Task ThrowsOnEachExecution()
    {
        using var data = BuildData();
        for (var i = 0; i < 2; i++)
        {
            await Assert.ThrowsExactlyAsync<Exception>(() =>
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

    // Regression test: projecting into a constructor where a value is implicitly
    // converted to a wider/nullable parameter type (here int -> int?) must not
    // throw. EntityFinder looks past such conversions to check whether what is
    // underneath is an entity, but was discarding the conversion from the
    // expression it then returned. Rebuilding the containing NewExpression from
    // that returned argument threw ArgumentException, even though this
    // projection contains no Include or tracking option for the detector to
    // legitimately flag
    [Test]
    public async Task ProjectionIntoConstructorWithNullableParameter()
    {
        await using var data = BuildData();
        await data.Employees
            .Select(_ => new EmployeeSummary(_.Name, _.Age))
            .ToListAsync();
    }

    [Test]
    public async Task NotEnabled()
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(nameof(NotEnabled));
        builder.EnableServiceProviderCaching(false);
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

    // a navigation is an entity, so projecting one returns entities that EF tracks
    [Test]
    public async Task AsNoTrackingThenProjectionOfNavigation()
    {
        await using var data = BuildData();
        await data.Employees
            .AsNoTracking()
            .Select(_ => _.Company)
            .ToListAsync();
        await data.Companies
            .AsNoTracking()
            .Select(_ => _.Employees)
            .ToListAsync();
        await data.Employees
            .AsNoTracking()
            .Select(_ => new
            {
                _.Id,
                _.Company
            })
            .ToListAsync();
        await data.Employees
            .Select(_ => _.Company)
            .AsNoTracking()
            .ToListAsync();
    }

    // only a member of the navigation is projected, so no entity is returned
    [Test]
    public async Task AsNoTrackingThenProjectionOfNavigationMember()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Employees
                    .AsNoTracking()
                    .Select(_ => new
                    {
                        _.Id,
                        CompanyName = _.Company!.Name
                    })
                    .ToListAsync())
            .IgnoreStackTrace();
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

        builder.EnableServiceProviderCaching(false);
        await using var data = new SampleDbContext(builder.Options);
        data.Companies
            .Take(10)
            .ToQueryString();
    }

    [Test]
    public async Task EnabledByEnableRecording()
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(nameof(EnabledByEnableRecording));
        builder.EnableRecording();
        builder.EnableServiceProviderCaching(false);
        using var data = new SampleDbContext(builder.Options);

        await Assert.ThrowsExactlyAsync<Exception>(() =>
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

        builder.EnableServiceProviderCaching(false);
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

    // a model anti-pattern, logged when the model is built
    [Test]
    public async Task BoolWithDefault()
    {
        var builder = new DbContextOptionsBuilder<BoolWithDefaultContext>();
        builder.UseSqlServer(connectionString);
        builder.ThrowOnAntiPatterns();
        builder.EnableServiceProviderCaching(false);
        await using var data = new BoolWithDefaultContext(builder.Options);

        await Throws(() => data.Model)
            .IgnoreStackTrace();
    }

    class BoolWithDefaultContext(DbContextOptions options) :
        DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder model) =>
            model
                .Entity<Flagged>()
                .Property(_ => _.Enabled)
                .HasDefaultValueSql("1");
    }

    class Flagged
    {
        public int Id { get; set; }
        public bool Enabled { get; set; }
    }

    [Test]
    public async Task KeyWithDefault()
    {
        var builder = new DbContextOptionsBuilder<KeyWithDefaultContext>();
        builder.UseSqlServer(connectionString);
        builder.ThrowOnAntiPatterns();
        builder.EnableServiceProviderCaching(false);
        await using var data = new KeyWithDefaultContext(builder.Options);

        await Throws(() => data.Model)
            .IgnoreStackTrace();
    }

    class KeyWithDefaultContext(DbContextOptions options) :
        DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder model) =>
            model
                .Entity<Keyed>()
                .Property(_ => _.Id)
                .HasDefaultValue(1);
    }

    class Keyed
    {
        public int Id { get; set; }
    }

    [Test]
    public async Task OptionalDependentWithoutIdentifyingProperty()
    {
        var builder = new DbContextOptionsBuilder<OptionalDependentContext>();
        builder.UseSqlServer(connectionString);
        builder.ThrowOnAntiPatterns();
        builder.EnableServiceProviderCaching(false);
        await using var data = new OptionalDependentContext(builder.Options);

        await Throws(() => data.Model)
            .IgnoreStackTrace();
    }

    class OptionalDependentContext(DbContextOptions options) :
        DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder model) =>
            model
                .Entity<Person>()
                .OwnsOne(_ => _.Address);
    }

    class Person
    {
        public int Id { get; set; }
        public Address? Address { get; set; }
    }

    class Address
    {
        public string? City { get; set; }
    }

    [Test]
    public async Task OrderByThenCount()
    {
        await using var data = BuildData();

        #region OrderByThenCount

        await ThrowsTask(() =>
                data.Companies
                    .OrderBy(_ => _.Name)
                    .CountAsync())
            .IgnoreStackTrace();

        #endregion
    }

    [Test]
    public async Task OrderByThenWhereThenAny()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Companies
                    .OrderBy(_ => _.Name)
                    .Where(_ => _.Name != "")
                    .AnyAsync())
            .IgnoreStackTrace();
    }

    [Test]
    public async Task OrderByThenCountInProjection()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Companies
                    .Select(_ => new
                    {
                        _.Name,
                        Count = _.Employees
                            .OrderBy(_ => _.Age)
                            .Count()
                    })
                    .ToListAsync())
            .IgnoreStackTrace();
    }

    // the ordering selects which rows Take counts
    [Test]
    public async Task OrderByThenTakeThenCount()
    {
        await using var data = BuildData();
        await data.Companies
            .OrderBy(_ => _.Name)
            .Take(10)
            .CountAsync();
    }

    [Test]
    public async Task OrderByThenSingle()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Companies
                    .OrderBy(_ => _.Name)
                    .SingleOrDefaultAsync(_ => _.Id == 1))
            .IgnoreStackTrace();
    }

    [Test]
    public async Task OrderByThenSingleInProjection()
    {
        await using var data = BuildData();
        await Assert.ThrowsExactlyAsync<Exception>(() =>
            data.Companies
                .Select(_ => _.Employees
                    .OrderBy(_ => _.Age)
                    .Select(_ => _.Name)
                    .Single())
                .ToListAsync());
    }

    // the ordering selects which row Take keeps
    [Test]
    public async Task OrderByThenTakeThenSingle()
    {
        await using var data = BuildData();
        await data.Companies
            .OrderBy(_ => _.Name)
            .Take(1)
            .SingleOrDefaultAsync();
    }

    [Test]
    public async Task CountGreaterThanZero()
    {
        await using var data = BuildData();

        #region CountGreaterThanZero

        await ThrowsTask(() =>
                data.Companies
                    .Where(_ => _.Employees.Count() > 0)
                    .ToListAsync())
            .IgnoreStackTrace();

        #endregion
    }

    [Test]
    public async Task CountEqualsZero()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Companies
                    .Where(_ => _.Employees.Count(_ => _.Age > 30) == 0)
                    .ToListAsync())
            .IgnoreStackTrace();
    }

    [Test]
    public async Task CountPropertyReversed()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Companies
                    .Where(_ => 1 <= _.Employees.Count)
                    .ToListAsync())
            .IgnoreStackTrace();
    }

    // the count itself is needed
    [Test]
    public async Task CountComparedToOtherValue()
    {
        await using var data = BuildData();
        await data.Companies
            .Where(_ => _.Employees.Count() > 1 && _.Employees.Count >= 2)
            .ToListAsync();
    }

    [Test]
    public async Task DistinctOnKey()
    {
        await using var data = BuildData();

        #region DistinctOnKey

        await ThrowsTask(() =>
                data.Companies
                    .Where(_ => _.Name != "")
                    .Select(_ => new
                    {
                        _.Id,
                        _.Name
                    })
                    .Distinct()
                    .ToListAsync())
            .IgnoreStackTrace();

        #endregion
    }

    [Test]
    public async Task DistinctOnEntity()
    {
        await using var data = BuildData();
        await ThrowsTask(() =>
                data.Companies
                    .Include(_ => _.Employees)
                    .Distinct()
                    .ToListAsync())
            .IgnoreStackTrace();
    }

    [Test]
    public async Task DistinctOnKeyOnly()
    {
        await using var data = BuildData();
        await Assert.ThrowsExactlyAsync<Exception>(() =>
            data.Employees
                .Select(_ => _.Id)
                .Distinct()
                .ToListAsync());
    }

    [Test]
    public async Task DistinctOnNavigationKey()
    {
        await using var data = BuildData();
        await Assert.ThrowsExactlyAsync<Exception>(() =>
            data.Companies
                .Select(_ => new
                {
                    _.Name,
                    Ids = _.Employees
                        .Select(_ => _.Id)
                        .Distinct()
                        .ToList()
                })
                .ToListAsync());
    }

    [Test]
    public async Task DistinctKept()
    {
        await using var data = BuildData();

        // without the key, rows can repeat
        await data.Employees
            .Select(_ => _.Name)
            .Distinct()
            .ToListAsync();

        // an employee's CompanyId repeats
        await data.Employees
            .Select(_ => new
            {
                _.CompanyId
            })
            .Distinct()
            .ToListAsync();

        // SelectMany returns a row per employee, so a company can repeat
        await data.Companies
            .SelectMany(_ => _.Employees, (company, employee) => company.Id)
            .Distinct()
            .ToListAsync();

        // GroupBy returns rows for the groups, not the employees
        await data.Employees
            .GroupBy(_ => _.CompanyId)
            .Select(_ => _.Count())
            .Distinct()
            .ToListAsync();
    }

    [Test]
    public async Task GroupByOnlyKey()
    {
        await using var data = BuildData();

        #region GroupByOnlyKey

        await ThrowsTask(() =>
                data.Employees
                    .GroupBy(_ => _.CompanyId)
                    .Select(_ => _.Key)
                    .ToListAsync())
            .IgnoreStackTrace();

        #endregion
    }

    [Test]
    public async Task GroupByOnlyKeyMembers()
    {
        await using var data = BuildData();
        await Assert.ThrowsExactlyAsync<Exception>(() =>
            data.Employees
                .GroupBy(_ => new
                {
                    _.CompanyId,
                    _.Age
                })
                .Select(_ => new
                {
                    _.Key.CompanyId,
                    _.Key.Age
                })
                .ToListAsync());
    }

    [Test]
    public async Task GroupByResultSelectorIgnoresElements()
    {
        await using var data = BuildData();
        await Assert.ThrowsExactlyAsync<Exception>(() =>
            data.Employees
                .GroupBy(_ => _.CompanyId, (companyId, employees) => companyId)
                .ToListAsync());
    }

    [Test]
    public async Task GroupByUsesGroups()
    {
        await using var data = BuildData();
        await data.Employees
            .GroupBy(_ => _.CompanyId)
            .Select(_ => new
            {
                _.Key,
                Count = _.Count()
            })
            .ToListAsync();
        await data.Employees
            .GroupBy(_ => _.CompanyId, (companyId, employees) => employees.Count())
            .ToListAsync();
    }

    [Test]
    public async Task UnorderedFirstInProjection()
    {
        await using var data = BuildData();

        #region UnorderedFirst

        await ThrowsTask(() =>
                data.Companies
                    .OrderBy(_ => _.Id)
                    .Select(_ => new
                    {
                        _.Name,
                        FirstEmployee = _.Employees.FirstOrDefault()!.Name
                    })
                    .ToListAsync())
            .IgnoreStackTrace();

        #endregion
    }

    [Test]
    public async Task UnorderedFirstInWhere()
    {
        await using var data = BuildData();
        await Assert.ThrowsExactlyAsync<Exception>(() =>
            data.Companies
                .Where(_ => _.Employees.First().Age > 30)
                .ToListAsync());
    }

    [Test]
    public async Task UnorderedFirstAfterSelect()
    {
        await using var data = BuildData();
        await Assert.ThrowsExactlyAsync<Exception>(() =>
            data.Companies
                .Select(_ => _.Employees
                    .Select(_ => _.Name)
                    .Distinct()
                    .FirstOrDefault())
                .ToListAsync());
    }

    [Test]
    public async Task OrderedOrFilteredFirstKept()
    {
        await using var data = BuildData();

        // ordered
        await data.Companies
            .Select(_ => _.Employees
                .OrderBy(_ => _.Name)
                .Select(_ => _.Name)
                .FirstOrDefault())
            .ToListAsync();

        // filtered, by a Where or a predicate
        await data.Companies
            .Select(_ => _.Employees
                .Where(_ => _.Age > 30)
                .Select(_ => _.Name)
                .FirstOrDefault())
            .ToListAsync();
        await data.Companies
            .Select(_ => _.Employees.FirstOrDefault(_ => _.Age > 30))
            .ToListAsync();

        // EF orders the groups by key
        await data.Employees
            .GroupBy(_ => _.CompanyId)
            .Select(_ => _.First())
            .ToListAsync();

        // the root query is left to EF
        await data.Companies
            .OrderBy(_ => _.Id)
            .FirstOrDefaultAsync();
    }

    // EF does not log FirstWithoutOrderByAndFilterWarning for First on a navigation in a subquery, which is why
    // UnorderedFirstDetector exists. When this fails, EF logs it, and the detector can be removed.
    [Test]
    public void EfIgnoresUnorderedFirstInSubquery()
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseSqlServer(connectionString);
        builder.ConfigureWarnings(_ => _.Throw(CoreEventId.FirstWithoutOrderByAndFilterWarning));
        builder.EnableServiceProviderCaching(false);
        using var data = new SampleDbContext(builder.Options);

        var query = data.Companies
            .OrderBy(_ => _.Id)
            .Select(_ => new
            {
                _.Name,
                FirstEmployee = _.Employees.FirstOrDefault()!.Name
            });
        try
        {
            query.ToQueryString();
        }
        catch (InvalidOperationException exception)
            when (exception.Message.Contains(nameof(CoreEventId.FirstWithoutOrderByAndFilterWarning)))
        {
            throw new(
                """
                You can remove UnorderedFirstDetector, since https://github.com/dotnet/efcore/issues/39129 is fixed.
                EF now logs FirstWithoutOrderByAndFilterWarning for First on a collection navigation in a subquery.
                """);
        }
    }

    // the queries are the case conversions the check detects
#pragma warning disable CA1862
    [Test]
    public async Task ToLowerInWhere()
    {
        await using var data = BuildCaseConversionData();

        #region ToLowerInWhere

        await ThrowsTask(() =>
                data.Companies
                    .Where(_ => _.Name.ToLower() == "company1")
                    .ToListAsync())
            .IgnoreStackTrace();

        #endregion
    }

    [Test]
    public async Task ToUpperInvariantInOrderBy()
    {
        await using var data = BuildCaseConversionData();
        await ThrowsTask(() =>
                data.Companies
                    .OrderBy(_ => _.Name.ToUpperInvariant())
                    .ToListAsync())
            .IgnoreStackTrace();
    }

    [Test]
    public async Task ToLowerInNestedPredicate()
    {
        await using var data = BuildCaseConversionData();
        await Assert.ThrowsExactlyAsync<Exception>(() =>
            data.Companies
                .Where(_ => _.Employees.Any(employee => employee.Company.Name.ToLower() == "company1"))
                .ToListAsync());
    }

    [Test]
    public async Task CaseConversionKept()
    {
        await using var data = BuildCaseConversionData();

        // a variable is sent as a parameter
        var name = "Company1";
        await data.Companies
            .Where(_ => _.Name == name.ToLower())
            .ToListAsync();

        // a projection only changes the output
        await data.Companies
            .Select(_ => _.Name.ToUpper())
            .ToListAsync();
    }

    [Test]
    public async Task CaseConversionNotEnabled()
    {
        await using var data = BuildData();
        await data.Companies
            .Where(_ => _.Name.ToLower() == "company1")
            .ToListAsync();
    }

    static SampleDbContext BuildCaseConversionData([CallerMemberName] string databaseName = "")
    {
        #region ThrowOnColumnCaseConversion

        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(databaseName);
        builder.ThrowOnAntiPatterns(_ => _.ThrowOnColumnCaseConversion = true);

        #endregion

        builder.EnableServiceProviderCaching(false);
        return new(builder.Options);
    }

#pragma warning restore CA1862

    [Test]
    public async Task DecimalWithoutPrecision()
    {
        var builder = new DbContextOptionsBuilder<DecimalContext>();
        builder.UseSqlServer(connectionString);
        builder.ThrowOnAntiPatterns();
        builder.EnableServiceProviderCaching(false);
        await using var data = new DecimalContext(builder.Options);

        await Throws(() => data.Model)
            .IgnoreStackTrace();
    }

    class DecimalContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<Priced> Items { get; set; } = null!;
    }

    class Priced
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
    }

    // TemporalAsOf and TemporalAll apply AsNoTracking themselves, so a projection after them is not flagged
    [Test]
    public void TemporalThenProjection()
    {
        using var data = BuildTemporalData();

        data.Items
            .TemporalAsOf(DateTime.UtcNow)
            .Select(_ => _.Name)
            .ToQueryString();
        data.Items
            .TemporalAll()
            .Select(_ => _.Id)
            .ToQueryString();
    }

    [Test]
    public async Task TemporalThenAsNoTrackingThenProjection()
    {
        using var data = BuildTemporalData();

        var query = data.Items
            .TemporalAsOf(DateTime.UtcNow)
            .AsNoTracking()
            .Select(_ => _.Name);
        var exception = Assert.ThrowsExactly<Exception>(() => query.ToQueryString());
        await Assert.That(exception!.Message).StartsWith("AsNoTracking() is ignored");
    }

    static TemporalContext BuildTemporalData()
    {
        var builder = new DbContextOptionsBuilder<TemporalContext>();
        builder.UseSqlServer(connectionString);
        builder.ThrowOnAntiPatterns();
        builder.EnableServiceProviderCaching(false);
        return new(builder.Options);
    }

    class TemporalContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<Versioned> Items { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) =>
            model
                .Entity<Versioned>()
                .ToTable(_ => _.IsTemporal());
    }

    class Versioned
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    [Test]
    public async Task RequiredNavigationWithQueryFilter()
    {
        var builder = new DbContextOptionsBuilder<QueryFilterContext>();
        builder.UseInMemoryDatabase(nameof(RequiredNavigationWithQueryFilter));
        builder.ThrowOnAntiPatterns();
        builder.EnableServiceProviderCaching(false);
        await using var data = new QueryFilterContext(builder.Options);

        await Throws(() => data.Model)
            .IgnoreStackTrace();
    }

    class QueryFilterContext(DbContextOptions options) :
        DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder model)
        {
            model
                .Entity<FilteredBlog>()
                .HasQueryFilter(_ => _.Active);
            model
                .Entity<FilteredPost>()
                .HasOne(_ => _.Blog)
                .WithMany()
                .IsRequired();
        }
    }

    class FilteredBlog
    {
        public int Id { get; set; }
        public bool Active { get; set; }
    }

    class FilteredPost
    {
        public int Id { get; set; }
        public FilteredBlog Blog { get; set; } = null!;
    }

    // logged by SaveChanges, while it builds the commands, so before connecting
    [Test]
    public async Task OptionalDependentWithAllNullProperties()
    {
        var builder = new DbContextOptionsBuilder<OptionalDependentContext>();
        builder.UseSqlServer("Server=unreachable;Database=AntiPatterns;Trusted_Connection=True;Connect Timeout=1");
        builder.ThrowOnAntiPatterns();
        builder.ConfigureWarnings(_ =>
            _.Ignore(RelationalEventId.OptionalDependentWithoutIdentifyingPropertyWarning));
        builder.EnableServiceProviderCaching(false);
        await using var data = new OptionalDependentContext(builder.Options);

        data.Add(
            new Person
            {
                Id = 1,
                Address = new()
            });
        await ThrowsTask(() => data.SaveChangesAsync())
            .IgnoreStackTrace();
    }

    [Test]
    public async Task CollectionFilterOutsideInclude()
    {
        await using var data = BuildCollectionFilterData();

        #region CollectionFilterOutsideInclude

        await ThrowsTask(() =>
                data.Companies
                    .Include(_ => _.Employees)
                    .Where(_ => _.Employees.Any(_ => _.Age > 30))
                    .ToListAsync())
            .IgnoreStackTrace();

        #endregion
    }

    [Test]
    public async Task CollectionFilterOutsideIncludeKept()
    {
        await using var data = BuildCollectionFilterData();

        // the Include already filters what it loads
        await data.Companies
            .Include(_ => _.Employees.Where(_ => _.Age > 30))
            .Where(_ => _.Employees.Any(_ => _.Age > 30))
            .ToListAsync();

        // the Where does not read the included collection
        await data.Companies
            .Include(_ => _.Employees)
            .Where(_ => _.Name != "")
            .ToListAsync();
    }

    [Test]
    public async Task CollectionFilterOutsideIncludeNotEnabled()
    {
        await using var data = BuildData();
        await data.Companies
            .Include(_ => _.Employees)
            .Where(_ => _.Employees.Any(_ => _.Age > 30))
            .ToListAsync();
    }

    static SampleDbContext BuildCollectionFilterData([CallerMemberName] string databaseName = "")
    {
        #region ThrowOnCollectionFilterOutsideInclude

        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(databaseName);
        builder.ThrowOnAntiPatterns(_ => _.ThrowOnCollectionFilterOutsideInclude = true);

        #endregion

        builder.EnableServiceProviderCaching(false);
        return new(builder.Options);
    }

    static SampleDbContext BuildSqlServerData()
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseSqlServer(connectionString);
        builder.ThrowOnAntiPatterns();
        builder.EnableServiceProviderCaching(false);
        return new(builder.Options);
    }

    static SampleDbContext BuildData([CallerMemberName] string databaseName = "")
    {
        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseInMemoryDatabase(databaseName);
        builder.ThrowOnAntiPatterns();
        builder.EnableServiceProviderCaching(false);
        return new(builder.Options);
    }
}

record EmployeeSummary(string Name, int? Age);
