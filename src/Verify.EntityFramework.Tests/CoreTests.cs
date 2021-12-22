using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using VerifyTests.EntityFramework;

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
        await Verify(data.ChangeTracker);
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
        await Verify(data.ChangeTracker);
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
        await Verify(data.ChangeTracker);
    }

    #endregion

    #region IgnoreNavigationProperties

    [Test]
    public async Task IgnoreNavigationProperties()
    {
        var options = DbContextOptions();

        await using var data = new SampleDbContext(options);

        var company = new Company
        {
            Content = "company"
        };
        var employee = new Employee
        {
            Content = "employee",
            Company = company
        };
        await Verify(employee)
            .ModifySerialization(
                x => x.IgnoreNavigationProperties(data));
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
        await Verify(data.ChangeTracker);
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
        await Verify(data.ChangeTracker);
    }

    [Test]
    public async Task UpdateEntity()
    {
        var options = DbContextOptions();

        await using var data = new SampleDbContext(options);
        data.Add(new Employee
        {
            Content = "before"
        });
        await data.SaveChangesAsync();

        var employee = data.Employees.Single();
        data.Update(employee).Entity.Content = "after";
        await Verify(data.ChangeTracker);
    }

    [Test]
    public async Task AllData()
    {
        var database = await DbContextBuilder.GetDatabase("AllData");
        var data = database.Context;

        #region AllData

        await Verify(data.AllData())
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
        await Verify(queryable);

        #endregion
    }


    [Test]
    public async Task NestedQueryable()
    {
        var database = await DbContextBuilder.GetDatabase("NestedQueryable");
        var data = database.Context;
        var queryable = data.Companies
            .Where(x => x.Content == "value");
        await Verify(new {queryable});
    }

    void Build(string connection)
    {
        #region EnableRecording

        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseSqlServer(connection);
        builder.EnableRecording();
        var data = new SampleDbContext(builder.Options);

        #endregion
    }

    [Test]
    public async Task MultiRecording()
    {
        var database = await DbContextBuilder.GetDatabase("MultiRecording");
        var data = database.Context;
        EfRecording.StartRecording();
        var company = new Company
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

        var company2 = new Company
        {
            Id = 2,
            Content = "Title2"
        };
        data.Add(company2);
        await data.SaveChangesAsync();

        var eventData = EfRecording.FinishRecording();
        await Verify(eventData);
    }

    [Test]
    public async Task MultiDbContexts()
    {
        var database = await DbContextBuilder.GetDatabase("MultiDbContexts");
        var connectionString = database.ConnectionString;

        #region MultiDbContexts

        var builder = new DbContextOptionsBuilder<SampleDbContext>();
        builder.UseSqlServer(connectionString);
        builder.EnableRecording();

        await using var data1 = new SampleDbContext(builder.Options);
        EfRecording.StartRecording();
        var company = new Company
        {
            Content = "Title"
        };
        data1.Add(company);
        await data1.SaveChangesAsync();

        await using var data2 = new SampleDbContext(builder.Options);
        await data2.Companies
            .Where(x => x.Content == "Title")
            .ToListAsync();

        await Verify(data2.Companies.Count());

        #endregion
    }

    [Test]
    public async Task Recording()
    {
        var database = await DbContextBuilder.GetDatabase("Recording");
        var data = database.Context;

        #region Recording

        var company = new Company
        {
            Content = "Title"
        };
        data.Add(company);
        await data.SaveChangesAsync();

        EfRecording.StartRecording();

        await data.Companies
            .Where(x => x.Content == "Title")
            .ToListAsync();

        await Verify(data.Companies.Count());

        #endregion
    }

    [Test]
    public async Task RecordingSpecific()
    {
        var database = await DbContextBuilder.GetDatabase("RecordingSpecific");
        var data = database.Context;

        #region RecordingSpecific

        var company = new Company
        {
            Content = "Title"
        };
        data.Add(company);
        await data.SaveChangesAsync();

        EfRecording.StartRecording();

        await data.Companies
            .Where(x => x.Content == "Title")
            .ToListAsync();

        var entries = EfRecording.FinishRecording();
        //TODO: optionally filter the results
        await Verify(new
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