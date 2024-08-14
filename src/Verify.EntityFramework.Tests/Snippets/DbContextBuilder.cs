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
        orderRequiredSqlInstance = new(
            buildTemplate: CreateDb,
            storage: Storage.FromSuffix<SampleDbContext>("ThrowForMissingOrderBy"),
            constructInstance: builder =>
            {
                builder.EnableRecording();
                builder.ThrowForMissingOrderBy();
                return new(builder.Options);
            });
    }

    static SqlInstance<SampleDbContext> sqlInstance;
    static SqlInstance<SampleDbContext> orderRequiredSqlInstance;

    static async Task CreateDb(SampleDbContext data)
    {
        await data.Database.EnsureCreatedAsync();

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
        data.AddRange(company1, employee1, employee2, company2, company3, company4, employee4);
        await data.SaveChangesAsync();
    }

    public static Task<SqlDatabase<SampleDbContext>> GetDatabase([CallerMemberName] string suffix = "")
        => sqlInstance.Build(suffix);

    public static Task<SqlDatabase<SampleDbContext>> GetOrderRequiredDatabase([CallerMemberName] string suffix = "")
        => orderRequiredSqlInstance.Build(suffix);
}