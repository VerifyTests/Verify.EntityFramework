[TestFixture]
public class DbUpdateExceptionTests
{
    [Test]
    public async Task Run()
    {
        var instance = new SqlInstance<TestDbContext>(builder => new(builder.Options));
        var id = Guid.NewGuid();
        var entity = new TestEntity
        {
            Id = id,
            Property = "Item1"
        };
        await using var database = await instance.Build(new List<object>
        {
            entity
        });

        var duplicate = new TestEntity
        {
            Id = id,
            Property = "Item1"
        };
        var testDbContext = database.NewDbContext();
        testDbContext.Add(duplicate);
        await ThrowsTask(() => testDbContext.SaveChangesAsync())
            .IgnoreStackTrace()
            .ScrubInlineGuids();
    }

    public class TestEntity
    {
        public Guid Id { get; set; }
        public string? Property { get; set; }
    }

    public class TestDbContext :
        DbContext
    {
        public DbSet<TestEntity> TestEntities { get; set; } = null!;

        public TestDbContext(DbContextOptions options) :
            base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder model) => model.Entity<TestEntity>();
    }
}