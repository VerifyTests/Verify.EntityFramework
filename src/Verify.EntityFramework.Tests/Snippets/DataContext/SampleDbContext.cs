public class SampleDbContext :
    DbContext
{
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<Company> Companies { get; set; } = null!;

    public SampleDbContext(DbContextOptions options) :
        base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Company>()
            .HasMany(_ => _.Employees)
            .WithOne(_ => _.Company)
            .IsRequired();
        builder.Entity<Employee>();
    }
}