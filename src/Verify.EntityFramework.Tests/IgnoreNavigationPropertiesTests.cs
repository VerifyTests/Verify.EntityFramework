[TestFixture]
[Parallelizable(ParallelScope.All)]
public class IgnoreNavigationPropertiesTests
{
    // owned types were ignored like any other navigation, so their data was dropped. The navigation from an owned
    // type back to its owner, and to another entity, is still ignored
    [Test]
    public async Task Owned()
    {
        var builder = new DbContextOptionsBuilder<OwnedDbContext>();
        builder.UseInMemoryDatabase(nameof(IgnoreNavigationPropertiesTests) + nameof(Owned));
        await using var data = new OwnedDbContext(builder.Options);

        var shop = new Shop
        {
            Id = 1,
            Name = "shop",
            Region = new()
            {
                Id = 1,
                Name = "region"
            }
        };
        shop.Address = new()
        {
            Street = "main st",
            Shop = shop
        };
        shop.Phones =
        [
            new()
            {
                Number = "123",
                Shop = shop
            }
        ];
        await Verify(shop)
            .IgnoreNavigationProperties(data);
    }

    public class OwnedDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<Shop> Shops { get; set; } = null!;
        public DbSet<Region> Regions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model)
        {
            var shop = model.Entity<Shop>();
            shop.OwnsOne(_ => _.Address, _ => _.WithOwner(_ => _.Shop));
            shop.OwnsMany(_ => _.Phones, _ => _.WithOwner(_ => _.Shop));
        }
    }

    public class Shop
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public required string Name { get; set; }
        public Address Address { get; set; } = null!;
        public List<Phone> Phones { get; set; } = [];
        public Region? Region { get; set; }
    }

    public class Address
    {
        public required string Street { get; set; }
        public Shop Shop { get; set; } = null!;
    }

    public class Phone
    {
        public required string Number { get; set; }
        public Shop Shop { get; set; } = null!;
    }

    public class Region
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public required string Name { get; set; }
    }

    // many-to-many navigations are skip navigations, and were not ignored
    [Test]
    public async Task ManyToMany()
    {
        var builder = new DbContextOptionsBuilder<ManyToManyDbContext>();
        builder.UseInMemoryDatabase(nameof(IgnoreNavigationPropertiesTests));
        await using var data = new ManyToManyDbContext(builder.Options);

        var post = new Post
        {
            Id = 1,
            Title = "post",
            Tags =
            [
                new()
                {
                    Id = 1,
                    Label = "tag"
                }
            ]
        };
        await Verify(post)
            .IgnoreNavigationProperties(data);
    }

    public class ManyToManyDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<Post> Posts { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
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
}
