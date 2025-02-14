﻿public class SampleDbContext(DbContextOptions options) :
    DbContext(options)
{
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<Company> Companies { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder model)
    {
        model
            .Entity<Company>()
            .HasMany(_ => _.Employees)
            .WithOne(_ => _.Company)
            .IsRequired();
        model.Entity<Employee>();
    }
}