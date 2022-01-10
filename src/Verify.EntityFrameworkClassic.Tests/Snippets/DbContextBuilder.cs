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

        var company1 = new Company
        {
            Id = 1,
            Content = "Company1"
        };
        var employee1 = new Employee
        {
            Id = 2,
            CompanyId = company1.Id,
            Content = "Employee1",
            Age = 25
        };
        var employee2 = new Employee
        {
            Id = 3,
            CompanyId = company1.Id,
            Content = "Employee2",
            Age = 31
        };
        var company2 = new Company
        {
            Id = 4,
            Content = "Company2"
        };
        var employee4 = new Employee
        {
            Id = 5,
            CompanyId = company2.Id,
            Content = "Employee4",
            Age = 34
        };
        var company3 = new Company
        {
            Id = 6,
            Content = "Company3"
        };
        var company4 = new Company
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