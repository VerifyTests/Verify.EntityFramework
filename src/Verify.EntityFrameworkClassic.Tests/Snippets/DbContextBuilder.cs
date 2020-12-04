using System.Threading.Tasks;
using EfLocalDb;

// LocalDb is used to make the sample simpler.
// Replace with a real DbContext
public static class DbContextBuilder
{
    static DbContextBuilder()
    {
        sqlInstance = new(
            buildTemplate: CreateDb,
            constructInstance: connection => new(connection),
            storage: Storage.FromSuffix<SampleDbContext>("Classic"));
    }

    static SqlInstance<SampleDbContext> sqlInstance;

    static async Task CreateDb(SampleDbContext data)
    {
        await data.CreateOnExistingDb();

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
        data.Companies.Add(company1);
        data.Companies.Add(company2);
        data.Companies.Add(company3);
        data.Companies.Add(company4);
        data.Employees.Add(employee1);
        data.Employees.Add(employee2);
        data.Employees.Add(employee4);
        await data.SaveChangesAsync();
    }

    public static Task<SqlDatabase<SampleDbContext>> GetDatabase(string suffix)
    {
        return sqlInstance.Build(suffix);
    }
}