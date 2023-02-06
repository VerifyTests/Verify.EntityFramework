[TestFixture]
public class ClassicTests
{
    static SqlInstance<SampleDbContext> sqlInstance;

    #region AddedClassic

    [Test]
    public async Task Added()
    {
        using var database = await sqlInstance.Build();
        var data = database.Context;
        data.Companies.Add(new() {Content = "before"});
        await Verify(data.ChangeTracker);
    }

    #endregion

    #region DeletedClassic

    [Test]
    public async Task Deleted()
    {
        using var database = await sqlInstance.Build();
        var data = database.Context;
        data.Companies.Add(new() {Content = "before"});
        await data.SaveChangesAsync();

        var company = data.Companies.Single();
        data.Companies.Remove(company);
        await Verify(data.ChangeTracker);
    }

    #endregion

    #region ModifiedClassic

    [Test]
    public async Task Modified()
    {
        using var database = await sqlInstance.Build();
        var data = database.Context;
        var company = new Company
        {
            Content = "before"
        };
        data.Companies.Add(company);
        await data.SaveChangesAsync();

        data.Companies.Single().Content = "after";
        await Verify(data.ChangeTracker);
    }

    #endregion

    [Test]
    public async Task WithNavigationProp()
    {
        using var database = await sqlInstance.Build();
        var data = database.Context;
        var company = new Company
        {
            Content = "companyBefore"
        };
        data.Companies.Add(company);
        var employee = new Employee
        {
            Content = "employeeBefore",
            Company = company
        };
        data.Employees.Add(employee);
        await data.SaveChangesAsync();

        data.Companies.Single().Content = "companyAfter";
        data.Employees.Single().Content = "employeeAfter";
        await Verify(data.ChangeTracker);
    }

    [Test, Explicit]
    public async Task SomePropsModified()
    {
        using var database = await sqlInstance.Build();
        var data = database.Context;
        var company = new Company
        {
            Content = "before"
        };
        data.Companies.Add(company);
        await data.SaveChangesAsync();
        var entity = data.Companies.Attach(new() {Id = company.Id});
        entity.Content = "after";
        data.Entry(entity).Property(_ => _.Content).IsModified = true;
        data.Configuration.ValidateOnSaveEnabled = false;
        await data.SaveChangesAsync();
        await Verify(data);
    }

    [Test]
    public async Task UpdateEntity()
    {
        using var database = await sqlInstance.Build();
        var data = database.Context;

        data.Companies.Add(new()
        {
            Content = "before"
        });
        await data.SaveChangesAsync();

        var company = data.Companies.Single();
        company.Content = "after";
        await Verify(data.ChangeTracker);
    }

    #region QueryableClassic

    [Test]
    public async Task Queryable()
    {
        var database = await DbContextBuilder.GetDatabase("Queryable");
        var data = database.Context;
        var queryable = data.Companies.Where(_ => _.Content == "value");
        await Verify(queryable);
    }

    #endregion

    static ClassicTests()
    {
        #region EnableClassic

        VerifyEntityFrameworkClassic.Initialize();

        #endregion

        sqlInstance = new(
            constructInstance: connection => new(connection),
            storage: Storage.FromSuffix<SampleDbContext>("Tests"));
    }
}