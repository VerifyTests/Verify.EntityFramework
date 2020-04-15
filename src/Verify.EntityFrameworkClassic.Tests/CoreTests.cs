using System.Linq;
using System.Threading.Tasks;
using EfLocalDb;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class CoreTests :
    VerifyBase
{
    static SqlInstance<SampleDbContext> sqlInstance;

    #region AddedClassic
    [Fact]
    public async Task Added()
    {
        using var database = await sqlInstance.Build();
        var data = database.Context;
        data.Companies.Add(new Company {Content = "before"});
        await Verify(data);
    }
    #endregion

    #region DeletedClassic
    [Fact]
    public async Task Deleted()
    {
        using var database = await sqlInstance.Build();
        var data = database.Context;
        data.Companies.Add(new Company {Content = "before"});
        await data.SaveChangesAsync();

        var company = data.Companies.Single();
        data.Companies.Remove(company);
        await Verify(data);
    }
    #endregion

    #region ModifiedClassic
    [Fact]
    public async Task Modified()
    {
        using var database = await sqlInstance.Build();
        var data = database.Context;
        var company = new Company {Content = "before"};
        data.Companies.Add(company);
        await data.SaveChangesAsync();

        data.Companies.Single().Content = "after";
        await Verify(data);
    }
    #endregion

    [Fact]
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
        await Verify(data);
    }

    [Fact(Skip = "TODO")]
    public async Task SomePropsModified()
    {
        using var database = await sqlInstance.Build();
        var data = database.Context;
        var company = new Company
        {
            Content = "before",
        };
        data.Companies.Add(company);
        await data.SaveChangesAsync();
        var entity = data.Companies.Attach(new Company {Id = company.Id});
        entity.Content = "after";
        data.Entry(entity).Property(_ => _.Content).IsModified = true;
        data.Configuration.ValidateOnSaveEnabled = false;
        await data.SaveChangesAsync();
        await Verify(data);
    }

    [Fact]
    public async Task UpdateEntity()
    {
        using var database = await sqlInstance.Build();
        var data = database.Context;

        data.Companies.Add(new Company
        {
            Content = "before",
        });
        await data.SaveChangesAsync();

        var company = data.Companies.Single();
        company.Content = "after";
        await Verify(data);
    }

    #region QueryableClassic
    [Fact]
    public async Task Queryable()
    {
        var database = await DbContextBuilder.GetDatabase("Queryable");
        var data = database.Context;
        var queryable = data.Companies.Where(x => x.Content == "value");
        await Verify(queryable);
    }
    #endregion

    public CoreTests(ITestOutputHelper output) :
        base(output)
    {
    }

    static CoreTests()
    {
        sqlInstance = new SqlInstance<SampleDbContext>(
            constructInstance: connection => new SampleDbContext(connection),instanceSuffix:"Tests");
    }
}