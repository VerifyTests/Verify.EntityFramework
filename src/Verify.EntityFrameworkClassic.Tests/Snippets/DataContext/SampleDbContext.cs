public class SampleDbContext(DbConnection connection) :
    DbContext(connection, false)
{
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<Company> Companies { get; set; } = null!;

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>();
        modelBuilder.Entity<Employee>();
    }
}