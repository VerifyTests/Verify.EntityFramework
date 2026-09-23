[TestFixture]
[Parallelizable(ParallelScope.All)]
public class AllDataTests
{
    // derived entities were returned once for each type in the hierarchy
    [Test]
    public async Task Inheritance()
    {
        await using var data = BuildData();
        data.Add(new Animal { Id = 2, Name = "generic" });
        data.Add(new Dog { Id = 1, Name = "rex", Breed = "lab" });
        await data.SaveChangesAsync();

        await Verify(data.AllData())
            .AddExtraSettings(_ => _.TypeNameHandling = TypeNameHandling.Objects);
    }

    // the implicit join entity is a shared-type entity, and threw when accessed via Set<T>()
    [Test]
    public async Task ManyToMany()
    {
        await using var data = BuildData();
        data.Add(
            new Post
            {
                Id = 1,
                Title = "post",
                Tags =
                [
                    new() { Id = 2, Label = "b" },
                    new() { Id = 1, Label = "a" }
                ]
            });
        await data.SaveChangesAsync();

        await Verify(data.AllData())
            .IgnoreNavigationProperties(data);
    }

    // ordered by the primary key, not a property named Id
    [Test]
    public async Task CompositeKey()
    {
        await using var data = BuildData();
        data.AddRange(
            new Membership { GroupKey = 2, UserKey = 1 },
            new Membership { GroupKey = 1, UserKey = 2 },
            new Membership { GroupKey = 1, UserKey = 1 });
        await data.SaveChangesAsync();

        await Verify(data.AllData());
    }

    static AllDataDbContext BuildData([CallerMemberName] string databaseName = "")
    {
        var builder = new DbContextOptionsBuilder<AllDataDbContext>();
        builder.UseInMemoryDatabase(nameof(AllDataTests) + databaseName);
        return new(builder.Options);
    }

    public class AllDataDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<Animal> Animals { get; set; } = null!;
        public DbSet<Dog> Dogs { get; set; } = null!;
        public DbSet<Post> Posts { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<Membership> Memberships { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) =>
            model
                .Entity<Membership>()
                .HasKey(_ => new { _.GroupKey, _.UserKey });
    }

    public class Animal
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public required string Name { get; set; }
    }

    public class Dog : Animal
    {
        public required string Breed { get; set; }
    }

    public class Post
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public required string Title { get; set; }
        public List<Tag> Tags { get; set; } = [];
    }

    public class Tag
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public required string Label { get; set; }
        public List<Post> Posts { get; set; } = [];
    }

    public class Membership
    {
        public int GroupKey { get; set; }
        public int UserKey { get; set; }
    }
}
