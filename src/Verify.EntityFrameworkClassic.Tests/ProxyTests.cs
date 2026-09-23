[TestFixture]
public class ProxyTests
{
    static SqlInstance<ProxyDbContext> sqlInstance = new(
        constructInstance: connection => new(connection),
        storage: Storage.FromSuffix<ProxyDbContext>("Proxy"));

    // with virtual navigations, an entity loaded from the database is a dynamic proxy,
    // which was written with the proxy type name
    [Test]
    public async Task Modified()
    {
        using var database = await sqlInstance.Build();
        var data = database.Context;
        data.Parents.Add(new()
        {
            Id = 1,
            Content = "before"
        });
        await data.SaveChangesAsync();

        using var data2 = new ProxyDbContext(database.Connection);
        var parent = data2.Parents.Single();
        Assert.That(parent.GetType(), Is.Not.EqualTo(typeof(Parent)));
        parent.Content = "after";
        await Verify(data2.ChangeTracker);
    }

    public class ProxyDbContext(DbConnection connection) :
        DbContext(connection, false)
    {
        public DbSet<Parent> Parents { get; set; } = null!;
        public DbSet<Child> Children { get; set; } = null!;
    }

    public class Parent
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public string? Content { get; set; }
        public virtual List<Child> Children { get; set; } = [];
    }

    public class Child
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public int ParentId { get; set; }
        public virtual Parent Parent { get; set; } = null!;
    }
}
