[TestFixture]
[Parallelizable(ParallelScope.All)]
public class DescriptiveParameterNamesTests
{
    static SqlInstance<DescriptiveDbContext> instance = new(
        constructInstance: builder =>
        {
            builder.EnableRecording();
            builder.UseDescriptiveParameterNames();
            return new(builder.Options);
        },
        storage: Storage.FromSuffix<DescriptiveDbContext>("DescriptiveParameterNamesTests"));

    // the counter fallback for a third Phone would be Phone2, which is already a column
    [Test]
    public async Task CounterCollidesWithColumn()
    {
        await using var database = await instance.Build();
        var data = database.Context;
        data.AddRange(
            new Contact { Id = 1, Phone = "a", Phone2 = "b" },
            new Contact { Id = 2, Phone = "c", Phone2 = "d" },
            new Contact { Id = 3, Phone = "e", Phone2 = "f" });
        Recording.Start();
        await data.SaveChangesAsync();
        await Verify();
    }

    // the implicit join entity has a clr type of Dictionary`2
    [Test]
    public async Task SharedTypeJoinEntity()
    {
        await using var database = await instance.Build();
        var data = database.Context;
        data.Add(
            new Post
            {
                Id = 1,
                Title = "post",
                Tags =
                [
                    new() { Id = 1, Label = "a" },
                    new() { Id = 2, Label = "b" }
                ]
            });
        Recording.Start();
        await data.SaveChangesAsync();
        await Verify();
    }

    [Test]
    public async Task ColumnNameWithSpace()
    {
        await using var database = await instance.Build();
        var data = database.Context;
        data.Add(new Person { Id = 1, FirstName = "name" });
        Recording.Start();
        await data.SaveChangesAsync();
        await Verify();
    }

    public class DescriptiveDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<Contact> Contacts { get; set; } = null!;
        public DbSet<Post> Posts { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<Person> People { get; set; } = null!;
    }

    public class Contact
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public required string Phone { get; set; }
        public required string Phone2 { get; set; }
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

    public class Person
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        [Column("First Name")]
        public required string FirstName { get; set; }
    }
}
