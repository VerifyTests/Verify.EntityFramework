using EfLocalDb;
using VerifyTests.EntityFramework;

// LocalDb is used to make the sample simpler.
// Replace with a real DbContext
public static class DbContextBuilder
{
    static DbContextBuilder()
    {
        sqlInstance = new(
            buildTemplate: CreateDb,
            constructInstance: builder =>
            {
                builder.EnableRecording();
                return new(builder.Options);
            });
    }

    static SqlInstance<SampleDbContext> sqlInstance;

    static async Task CreateDb(SampleDbContext data)
    {
        await data.Database.EnsureCreatedAsync();

        Company company1 = new()
        {
            Id = 1,
            Content = "Company1"
        };
        Employee employee1 = new()
        {
            Id = 2,
            CompanyId = company1.Id,
            Content = "Employee1",
            Age = 25
        };
        Employee employee2 = new()
        {
            Id = 3,
            CompanyId = company1.Id,
            Content = "Employee2",
            Age = 31
        };
        Company company2 = new()
        {
            Id = 4,
            Content = "Company2"
        };
        Employee employee4 = new()
        {
            Id = 5,
            CompanyId = company2.Id,
            Content = "Employee4",
            Age = 34
        };
        Company company3 = new()
        {
            Id = 6,
            Content = "Company3"
        };
        Company company4 = new()
        {
            Id = 7,
            Content = "Company4"
        };
        data.AddRange(company1, employee1, employee2, company2, company3, company4, employee4);
        await data.SaveChangesAsync();
    }

    public static Task<SqlDatabase<SampleDbContext>> GetDatabase(string suffix)
    {
        return sqlInstance.Build(suffix);
    }
}