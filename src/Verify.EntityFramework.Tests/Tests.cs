using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class Tests :
    VerifyBase
{
    #region Added
    [Fact]
    public async Task Added()
    {
        var options = DbContextOptions();

        await using var data = new SampleDbContext(options);
        data.Add(new Company {Content = "before"});
        await Verify(data);
    }
    #endregion

    #region Deleted
    [Fact]
    public async Task Deleted()
    {
        var options = DbContextOptions();

        await using var data = new SampleDbContext(options);
        data.Add(new Company {Content = "before"});
        await data.SaveChangesAsync();

        var company = data.Companies.Single();
        data.Companies.Remove(company);
        await Verify(data);
    }
    #endregion

    #region Modified
    [Fact]
    public async Task Modified()
    {
        var options = DbContextOptions();

        await using var data = new SampleDbContext(options);
        var company = new Company {Content = "before"};
        data.Add(company);
        await data.SaveChangesAsync();

        data.Companies.Single().Content = "after";
        await Verify(data);
    }
    #endregion

    [Fact]
    public async Task WithNavigationProp()
    {
        var options = DbContextOptions();

        await using var data = new SampleDbContext(options);
        var company = new Company
        {
            Content = "companyBefore"
        };
        data.Add(company);
        var employee = new Employee
        {
            Content = "employeeBefore",
            Company = company
        };
        data.Add(employee);
        await data.SaveChangesAsync();

        data.Companies.Single().Content = "companyAfter";
        data.Employees.Single().Content = "employeeAfter";
        await Verify(data);
    }

    [Fact]
    public async Task SomePropsModified()
    {
        var options = DbContextOptions();

        await using var data = new SampleDbContext(options);
        data.Add(new Employee
        {
            Content = "before",
            Age = 10
        });
        await data.SaveChangesAsync();

        data.Employees.Single().Content = "after";
        await Verify(data);
    }

    [Fact]
    public async Task UpdateEntity()
    {
        var options = DbContextOptions();

        await using var data = new SampleDbContext(options);
        data.Add(new Employee
        {
            Content = "before",
        });
        await data.SaveChangesAsync();

        var employee = data.Employees.Single();
        data.Update(employee).Entity.Content = "after";
        await Verify(data);
    }

    #region Queryable
    [Fact]
    public async Task Queryable()
    {
        var database = await DbContextBuilder.GetDatabase("Queryable");
        var data = database.Context;
        var queryable = data.Companies.Where(x => x.Content == "value");
        await Verify(queryable);
    }
    #endregion

    static DbContextOptions<SampleDbContext> DbContextOptions(
        [CallerMemberName] string databaseName = "")
    {
        return new DbContextOptionsBuilder<SampleDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
    }

    public Tests(ITestOutputHelper output) :
        base(output)
    {
    }
}