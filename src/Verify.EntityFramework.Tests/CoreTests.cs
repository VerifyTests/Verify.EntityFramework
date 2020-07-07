using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VerifyNUnit;
using NUnit.Framework;
using VerifyTests;

[TestFixture]
public class CoreTests
{
    #region Added
    [Test]
    public async Task Added()
    {
        var options = DbContextOptions();

        await using var data = new SampleDbContext(options);
        var company = new Company
        {
            Content = "before"
        };
        data.Add(company);
        await Verifier.Verify(data);
    }
    #endregion

    #region Deleted
    [Test]
    public async Task Deleted()
    {
        var options = DbContextOptions();

        await using var data = new SampleDbContext(options);
        data.Add(new Company {Content = "before"});
        await data.SaveChangesAsync();

        var company = data.Companies.Single();
        data.Companies.Remove(company);
        await Verifier.Verify(data);
    }
    #endregion

    #region Modified
    [Test]
    public async Task Modified()
    {
        var options = DbContextOptions();

        await using var data = new SampleDbContext(options);
        var company = new Company
        {
            Content = "before"
        };
        data.Add(company);
        await data.SaveChangesAsync();

        data.Companies.Single().Content = "after";
        await Verifier.Verify(data);
    }
    #endregion

    [Test]
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
        await Verifier.Verify(data);
    }

    [Test]
    public async Task SomePropsModified()
    {
        var options = DbContextOptions();

        await using var data = new SampleDbContext(options);
        var employee = new Employee
        {
            Content = "before",
            Age = 10
        };
        data.Add(employee);
        await data.SaveChangesAsync();

        data.Employees.Single().Content = "after";
        await Verifier.Verify(data);
    }

    [Test]
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
        await Verifier.Verify(data);
    }

    #region Queryable
    [Test]
    public async Task Queryable()
    {
        var database = await DbContextBuilder.GetDatabase("Queryable");
        var data = database.Context;
        var queryable = data.Companies
            .Where(x => x.Content == "value");
        await Verifier.Verify(queryable);
    }
    #endregion

    static DbContextOptions<SampleDbContext> DbContextOptions(
        [CallerMemberName] string databaseName = "")
    {
        return new DbContextOptionsBuilder<SampleDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
    }

    static CoreTests()
    {
        #region EnableCore
        VerifyEntityFramework.Enable();
        #endregion
    }
}