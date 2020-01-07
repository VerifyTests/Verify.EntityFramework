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

        await using var context = new SampleDbContext(options);
        context.Companies.Add(new Company {Content = "before"});
        await Verify(context);
    }
    #endregion

    #region Deleted
    [Fact]
    public async Task Deleted()
    {
        var options = DbContextOptions();

        await using (var context = new SampleDbContext(options))
        {
            context.Companies.Add(new Company {Content = "before"});
            context.SaveChanges();
        }

        await using (var context = new SampleDbContext(options))
        {
            var company = context.Companies.Single();
            context.Companies.Remove(company);
            await Verify(context);
        }
    }
    #endregion

    #region Modified
    [Fact]
    public async Task Modified()
    {
        var options = DbContextOptions();

        await using (var context = new SampleDbContext(options))
        {
            context.Add(new Company {Content = "before"});
            context.SaveChanges();
        }

        await using (var context = new SampleDbContext(options))
        {
            context.Companies.Single().Content = "after";
            await Verify(context);
        }
    }
    #endregion
    [Fact]
    public async Task WithNavigationProp()
    {
        var options = DbContextOptions();

        await using (var context = new SampleDbContext(options))
        {
            var company = new Company
            {
                Content = "companyBefore"
            };
            context.Add(company);
            context.Add(new Employee
            {
                Content = "employeeBefore",
                Company = company
            });
            context.SaveChanges();
        }

        await using (var context = new SampleDbContext(options))
        {
            context.Companies.Single().Content = "companyAfter";
            context.Employees.Single().Content = "employeeAfter";
            await Verify(context);
        }
    }
    [Fact]
    public async Task SomePropsModified()
    {
        var options = DbContextOptions();

        await using (var context = new SampleDbContext(options))
        {
            context.Add(new Employee
            {
                Content = "before",
                Age = 10
            });
            context.SaveChanges();
        }

        await using (var context = new SampleDbContext(options))
        {
            context.Employees.Single().Content = "after";
            await Verify(context);
        }
    }
    [Fact]
    public async Task UpdateEntity()
    {
        var options = DbContextOptions();

        await using (var context = new SampleDbContext(options))
        {
            context.Add(new Employee
            {
                Content = "before",
            });
            context.SaveChanges();
        }

        await using (var context = new SampleDbContext(options))
        {
            var employee = context.Employees.Single();
            context.Update(employee).Entity.Content = "after";
            await Verify(context);
        }
    }

    #region Queryable
    [Fact]
    public async Task Queryable()
    {
        var database = await DbContextBuilder.GetDatabase("Queryable");
        var dbContext = database.Context;
        var queryable = dbContext.Companies.Where(x => x.Content == "value");
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