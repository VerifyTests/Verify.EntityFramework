[TestFixture]
[Parallelizable(ParallelScope.All)]
public class NullableNavigationTests
{
    [Test]
    public async Task Include()
    {
        await using var data = await BuildData(nameof(Include));

        var cars = await data.Cars
            .Include(_ => _.Owner)
            .OrderBy(_ => _.Id)
            .ToListAsync();
        await Verify(cars);
    }

    [Test]
    public async Task IgnoreNavigationProperties()
    {
        await using var data = await BuildData(nameof(IgnoreNavigationProperties));

        var cars = await data.Cars
            .Include(_ => _.Owner)
            .OrderBy(_ => _.Id)
            .ToListAsync();
        await Verify(cars)
            .IgnoreNavigationProperties(data);
    }

    [Test]
    public async Task SuppressNull()
    {
        await using var data = await BuildData(nameof(SuppressNull));

        var cars = await data.Cars
            .Where(_ => _.Owner!.Name == "owner")
            .ToListAsync();
        await Verify(cars);
    }

    [Test]
    public async Task RedundantNullCheck()
    {
        await using var data = await BuildData(nameof(RedundantNullCheck));

        #region RedundantNullCheck

        await ThrowsTask(() =>
                data.Cars
                    .Where(_ => _.Owner != null && _.Owner.Name == "owner")
                    .ToListAsync())
            .IgnoreStackTrace();

        #endregion
    }

    [Test]
    public async Task RedundantNullCheckReversed()
    {
        await using var data = await BuildData(nameof(RedundantNullCheckReversed));

        Assert.ThrowsAsync<Exception>(() =>
            data.Cars
                .Where(_ => _.Owner!.Id > 0 && null != _.Owner)
                .ToListAsync());
    }

    // a null Owner matches `!=`, so the check changes the result
    [Test]
    public async Task NullCheckKeptForNotEqual()
    {
        await using var data = await BuildData(nameof(NullCheckKeptForNotEqual));

        var cars = await data.Cars
            .Where(_ => _.Owner != null && _.Owner.Name != "other")
            .ToListAsync();
        Assert.That(cars, Has.Count.EqualTo(1));
    }

    // a null Owner matches when the variable is null, so the check changes the result
    [Test]
    public async Task NullCheckKeptForVariable()
    {
        await using var data = await BuildData(nameof(NullCheckKeptForVariable));

        string? name = null;
        var cars = await data.Cars
            .Where(_ => _.Owner != null && _.Owner.Name == name)
            .ToListAsync();
        Assert.That(cars, Is.Empty);
    }

    [Test]
    public async Task AllData()
    {
        await using var data = await BuildData(nameof(AllData));

        await Verify(data.AllData())
            .AddExtraSettings(_ => _.TypeNameHandling = TypeNameHandling.Objects);
    }

    static async Task<NullableNavigationDbContext> BuildData(string name)
    {
        var builder = new DbContextOptionsBuilder<NullableNavigationDbContext>();
        builder.UseInMemoryDatabase(nameof(NullableNavigationTests) + name);
        builder.ThrowOnAntiPatterns();
        var data = new NullableNavigationDbContext(builder.Options);

        var owner = new Owner
        {
            Id = 1,
            Name = "owner"
        };
        data.AddRange(
            new Car
            {
                Id = 1,
                Model = "with owner",
                Owner = owner
            },
            new Car
            {
                Id = 2,
                Model = "without owner"
            });
        await data.SaveChangesAsync();
        data.ChangeTracker.Clear();
        return data;
    }

    public class NullableNavigationDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<Car> Cars { get; set; } = null!;
        public DbSet<Owner> Owners { get; set; } = null!;
    }

    public class Car
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public required string Model { get; set; }
        public int? OwnerId { get; set; }
        public Owner? Owner { get; set; }
    }

    public class Owner
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public required string Name { get; set; }
        public List<Car> Cars { get; set; } = [];
    }
}
