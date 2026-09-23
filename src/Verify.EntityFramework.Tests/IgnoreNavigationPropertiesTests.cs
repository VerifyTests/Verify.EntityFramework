[TestFixture]
[Parallelizable(ParallelScope.All)]
public class IgnoreNavigationPropertiesTests
{
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
