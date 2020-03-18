using System.Linq;
using System.Threading.Tasks;
using EfLocalDb;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class Tests :
    VerifyBase
{
    static SqlInstance<SampleDbContext> sqlInstance;

    #region AddedClassic
    [Fact]
    public async Task Added()
    {
        using var database = await sqlInstance.Build();
        var context = database.Context;
        context.Companies.Add(new Company {Content = "before"});
        await Verify(context);
    }
    #endregion

    #region DeletedClassic
    [Fact]
    public async Task Deleted()
    {
        using var database = await sqlInstance.Build();
        var context = database.Context;
        context.Companies.Add(new Company {Content = "before"});
        await context.SaveChangesAsync();

        var company = context.Companies.Single();
        context.Companies.Remove(company);
        await Verify(context);
    }
    #endregion

    #region ModifiedClassic
    [Fact]
    public async Task Modified()
    {
        using var database = await sqlInstance.Build();
        var context = database.Context;
        var company = new Company {Content = "before"};
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        context.Companies.Single().Content = "after";
        await Verify(context);
    }
    #endregion

    [Fact]
    public async Task WithNavigationProp()
    {
        using var database = await sqlInstance.Build();
        var context = database.Context;
        var company = new Company
        {
            Content = "companyBefore"
        };
        context.Companies.Add(company);
        var employee = new Employee
        {
            Content = "employeeBefore",
            Company = company
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        context.Companies.Single().Content = "companyAfter";
        context.Employees.Single().Content = "employeeAfter";
        await Verify(context);
    }

    [Fact(Skip = "TODO")]
    public async Task SomePropsModified()
    {
        using var database = await sqlInstance.Build();
        var context = database.Context;
        var company = new Company
        {
            Content = "before",
        };
        context.Companies.Add(company);
        await context.SaveChangesAsync();
        var entity = context.Companies.Attach(new Company {Id = company.Id});
        entity.Content = "after";
        context.Entry(entity).Property(_ => _.Content).IsModified = true;
        context.Configuration.ValidateOnSaveEnabled = false;
        await context.SaveChangesAsync();
        await Verify(context);
    }

    [Fact]
    public async Task UpdateEntity()
    {
        using var database = await sqlInstance.Build();
        var context = database.Context;

        context.Companies.Add(new Company
        {
            Content = "before",
        });
        await context.SaveChangesAsync();

        var company = context.Companies.Single();
        company.Content = "after";
        await Verify(context);
    }

    #region QueryableClassic
    [Fact]
    public async Task Queryable()
    {
        var database = await DbContextBuilder.GetDatabase("Queryable");
        var dbContext = database.Context;
        var queryable = dbContext.Companies.Where(x => x.Content == "value");
        await Verify(queryable);
    }
    #endregion

    public Tests(ITestOutputHelper output) :
        base(output)
    {
    }

    static Tests()
    {
        sqlInstance = new SqlInstance<SampleDbContext>(
            constructInstance: connection => new SampleDbContext(connection),instanceSuffix:"Tests");
    }
}