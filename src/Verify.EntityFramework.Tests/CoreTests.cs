using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using VerifyNUnit;
using NUnit.Framework;
using VerifyTests;
using VerifyTests.EntityFramework;

[TestFixture]
public class CoreTests
{
    #region Added

    [Test]
    public async Task Added()
    {
        var options = DbContextOptions();

        await using SampleDbContext data = new(options);
        Company company = new()
        {
            Content = "before"
        };
        data.Add(company);
        await Verifier.Verify(data.ChangeTracker);
    }

    #endregion

    #region Deleted

    [Test]
    public async Task Deleted()
    {
        var options = DbContextOptions();

        await using SampleDbContext data = new(options);
        data.Add(new Company {Content = "before"});
        await data.SaveChangesAsync();

        var company = data.Companies.Single();
        data.Companies.Remove(company);
        await Verifier.Verify(data.ChangeTracker);
    }

    #endregion

    #region Modified

    [Test]
    public async Task Modified()
    {
        var options = DbContextOptions();

        await using SampleDbContext data = new(options);
        Company company = new()
        {
            Content = "before"
        };
        data.Add(company);
        await data.SaveChangesAsync();

        data.Companies.Single().Content = "after";
        await Verifier.Verify(data.ChangeTracker);
    }

    #endregion

    [Test]
    public async Task WithNavigationProp()
    {
        var options = DbContextOptions();

        await using SampleDbContext data = new(options);
        Company company = new()
        {
            Content = "companyBefore"
        };
        data.Add(company);
        Employee employee = new()
        {
            Content = "employeeBefore",
            Company = company
        };
        data.Add(employee);
        await data.SaveChangesAsync();

        data.Companies.Single().Content = "companyAfter";
        data.Employees.Single().Content = "employeeAfter";
        await Verifier.Verify(data.ChangeTracker);
    }

    [Test]
    public async Task SomePropsModified()
    {
        var options = DbContextOptions();

        await using SampleDbContext data = new(options);
        Employee employee = new()
        {
            Content = "before",
            Age = 10
        };
        data.Add(employee);
        await data.SaveChangesAsync();

        data.Employees.Single().Content = "after";
        await Verifier.Verify(data.ChangeTracker);
    }

    [Test]
    public async Task UpdateEntity()
    {
        var options = DbContextOptions();

        await using SampleDbContext data = new(options);
        data.Add(new Employee
        {
            Content = "before"
        });
        await data.SaveChangesAsync();

        var employee = data.Employees.Single();
        data.Update(employee).Entity.Content = "after";
        await Verifier.Verify(data.ChangeTracker);
    }

    [Test]
    public async Task AllData()
    {
        var database = await DbContextBuilder.GetDatabase("AllData");
        var data = database.Context;

        #region AllData

        await Verifier.Verify(data.AllData())
            .ModifySerialization(
                serialization =>
                    serialization.AddExtraSettings(
                        serializer =>
                            serializer.TypeNameHandling = TypeNameHandling.Objects));

        #endregion
    }


    [Test]
    public async Task Queryable()
    {
        var database = await DbContextBuilder.GetDatabase("Queryable");
        var data = database.Context;

        #region Queryable

        var queryable = data.Companies
            .Where(x => x.Content == "value");
        await Verifier.Verify(queryable);

        #endregion
    }


    [Test]
    public async Task NestedQueryable()
    {
        var database = await DbContextBuilder.GetDatabase("NestedQueryable");
        var data = database.Context;
        var queryable = data.Companies
            .Where(x => x.Content == "value");
        await Verifier.Verify(new{queryable});
    }

    void Build(string connection)
    {
        #region EnableRecording

        DbContextOptionsBuilder<SampleDbContext> builder = new();
        builder.UseSqlServer(connection);
        builder.EnableRecording();
        SampleDbContext data = new(builder.Options);

        #endregion
    }

    [Test]
    public async Task MultiRecording()
    {
        var database = await DbContextBuilder.GetDatabase("MultiRecording");
        var data = database.Context;
        SqlRecording.StartRecording();
        Company company = new()
        {
            Content = "Title"
        };
        data.Add(company);
        await data.SaveChangesAsync();

        for (var i = 0; i < 100; i++)
        {
            var s = i.ToString();
            await data.Companies
                .Where(x => x.Content == s)
                .ToListAsync();
        }
        Company company2 = new()
        {
            Id = 2,
            Content = "Title2"
        };
        data.Add(company2);
        await data.SaveChangesAsync();

        var eventData = SqlRecording.FinishRecording();
        await Verifier.Verify(eventData);
    }

    [Test]
    public async Task MultiDbContexts()
    {
        var database = await DbContextBuilder.GetDatabase("MultiDbContexts");
        var connectionString = database.ConnectionString;

        #region MultiDbContexts

        DbContextOptionsBuilder<SampleDbContext> builder = new();
        builder.UseSqlServer(connectionString);
        builder.EnableRecording();

        await using SampleDbContext data1 = new(builder.Options);
        SqlRecording.StartRecording();
        Company company = new()
        {
            Content = "Title"
        };
        data1.Add(company);
        await data1.SaveChangesAsync();

        await using SampleDbContext data2 = new(builder.Options);
        await data2.Companies
            .Where(x => x.Content == "Title")
            .ToListAsync();

        await Verifier.Verify(data2.Companies.Count());

        #endregion
    }

    [Test]
    public async Task Recording()
    {
        var database = await DbContextBuilder.GetDatabase("Recording");
        var data = database.Context;

        #region Recording

        Company company = new()
        {
            Content = "Title"
        };
        data.Add(company);
        await data.SaveChangesAsync();

        SqlRecording.StartRecording();

        await data.Companies
            .Where(x => x.Content == "Title")
            .ToListAsync();

        await Verifier.Verify(data.Companies.Count());

        #endregion
    }

    [Test]
    public async Task RecordingSpecific()
    {
        var database = await DbContextBuilder.GetDatabase("RecordingSpecific");
        var data = database.Context;

        #region RecordingSpecific

        Company company = new()
        {
            Content = "Title"
        };
        data.Add(company);
        await data.SaveChangesAsync();

        SqlRecording.StartRecording();

        await data.Companies
            .Where(x => x.Content == "Title")
            .ToListAsync();

        var entries = SqlRecording.FinishRecording();
        //TODO: optionally filter the results
        await Verifier.Verify(new
        {
            target = data.Companies.Count(),
            sql = entries
        });

        #endregion
    }


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