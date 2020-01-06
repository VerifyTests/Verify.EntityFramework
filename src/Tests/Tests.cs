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
            context.Companies.Add(new Company {Content = "before"});
            context.SaveChanges();
        }

        await using (var context = new SampleDbContext(options))
        {
            context.Companies.Single().Content = "after";
            await Verify(context);
        }
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