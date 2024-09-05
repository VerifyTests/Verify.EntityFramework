# <img src="/src/icon.png" height="30px"> Verify.EntityFramework

[![Discussions](https://img.shields.io/badge/Verify-Discussions-yellow?svg=true&label=)](https://github.com/orgs/VerifyTests/discussions)
[![Build status](https://ci.appveyor.com/api/projects/status/g6njwv0aox62atu0?svg=true)](https://ci.appveyor.com/project/SimonCropp/verify-entityframework)
[![NuGet Status](https://img.shields.io/nuget/v/Verify.EntityFramework.svg?label=Verify.EntityFramework)](https://www.nuget.org/packages/Verify.EntityFramework/)
[![NuGet Status](https://img.shields.io/nuget/v/Verify.EntityFrameworkClassic.svg?label=Verify.EntityFrameworkClassic)](https://www.nuget.org/packages/Verify.EntityFrameworkClassic/)

Extends [Verify](https://github.com/VerifyTests/Verify) to allow snapshot testing with EntityFramework.

**See [Milestones](../../milestones?state=closed) for release notes.**


## NuGet packages

 * https://nuget.org/packages/Verify.EntityFramework/
 * https://nuget.org/packages/Verify.EntityFrameworkClassic/


## Enable

Enable VerifyEntityFramework once at assembly load time:


### EF Core

<!-- snippet: EnableCore -->
<a id='snippet-EnableCore'></a>
```cs
static IModel GetDbModel()
{
    var options = new DbContextOptionsBuilder<SampleDbContext>();
    options.UseSqlServer("fake");
    using var data = new SampleDbContext(options.Options);
    return data.Model;
}

[ModuleInitializer]
public static void Init()
{
    var model = GetDbModel();
    VerifyEntityFramework.Initialize(model);
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/ModuleInitializer.cs#L5-L22' title='Snippet source file'>snippet source</a> | <a href='#snippet-EnableCore' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `GetDbModel` pattern allows an instance of the `IModel` to be stored for use when `IgnoreNavigationProperties` is called inside tests. This is optional, and instead can be passed explicitly to `IgnoreNavigationProperties`.


### EF Classic

<!-- snippet: EnableClassic -->
<a id='snippet-EnableClassic'></a>
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyEntityFrameworkClassic.Initialize();
```
<sup><a href='/src/Verify.EntityFrameworkClassic.Tests/ModuleInitializer.cs#L3-L9' title='Snippet source file'>snippet source</a> | <a href='#snippet-EnableClassic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Recording

Recording allows all commands executed by EF to be captured and then (optionally) verified.


### Enable

Call `EfRecording.EnableRecording()` on `DbContextOptionsBuilder`.

<!-- snippet: EnableRecording -->
<a id='snippet-EnableRecording'></a>
```cs
var builder = new DbContextOptionsBuilder<SampleDbContext>();
builder.UseSqlServer(connection);
builder.EnableRecording();
var data = new SampleDbContext(builder.Options);
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L384-L391' title='Snippet source file'>snippet source</a> | <a href='#snippet-EnableRecording' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`EnableRecording` should only be called in the test context.


### Usage

To start recording call `EfRecording.StartRecording()`. The results will be automatically included in verified file.

<!-- snippet: Recording -->
<a id='snippet-Recording'></a>
```cs
var company = new Company
{
    Name = "Title"
};
data.Add(company);
await data.SaveChangesAsync();

Recording.Start();

await data
    .Companies
    .Where(_ => _.Name == "Title")
    .ToListAsync();

await Verify();
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L483-L501' title='Snippet source file'>snippet source</a> | <a href='#snippet-Recording' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Will result in the following verified file:

<!-- snippet: CoreTests.RecordingTest.verified.txt -->
<a id='snippet-CoreTests.RecordingTest.verified.txt'></a>
```txt
{
  ef: {
    Type: ReaderExecutedAsync,
    HasTransaction: false,
    Text:
SELECT [c].[Id], [c].[Name]
FROM [Companies] AS [c]
WHERE [c].[Name] = N'Title'
  }
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.RecordingTest.verified.txt#L1-L10' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.RecordingTest.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


Sql entries can be explicitly read using `EfRecording.FinishRecording`, optionally filtered, and passed to Verify:

<!-- snippet: RecordingSpecific -->
<a id='snippet-RecordingSpecific'></a>
```cs
var company = new Company
{
    Name = "Title"
};
data.Add(company);
await data.SaveChangesAsync();

Recording.Start();

await data
    .Companies
    .Where(_ => _.Name == "Title")
    .ToListAsync();

var entries = Recording.Stop();
//TODO: optionally filter the results
await Verify(
    new
    {
        target = data.Companies.Count(),
        entries
    });
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L639-L664' title='Snippet source file'>snippet source</a> | <a href='#snippet-RecordingSpecific' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### DbContext spanning

`StartRecording` can be called on different DbContext instances (built from the same options) and the results will be aggregated.

<!-- snippet: MultiDbContexts -->
<a id='snippet-MultiDbContexts'></a>
```cs
var builder = new DbContextOptionsBuilder<SampleDbContext>();
builder.UseSqlServer(connectionString);
builder.EnableRecording();

await using var data1 = new SampleDbContext(builder.Options);
Recording.Start();
var company = new Company
{
    Name = "Title"
};
data1.Add(company);
await data1.SaveChangesAsync();

await using var data2 = new SampleDbContext(builder.Options);
await data2
    .Companies
    .Where(_ => _.Name == "Title")
    .ToListAsync();

await Verify();
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L451-L474' title='Snippet source file'>snippet source</a> | <a href='#snippet-MultiDbContexts' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: CoreTests.MultiDbContexts.verified.txt -->
<a id='snippet-CoreTests.MultiDbContexts.verified.txt'></a>
```txt
{
  ef: [
    {
      Type: ReaderExecutedAsync,
      HasTransaction: false,
      Parameters: {
        @p0 (Int32): 0,
        @p1 (String): Title
      },
      Text:
SET IMPLICIT_TRANSACTIONS OFF;
SET NOCOUNT ON;
INSERT INTO [Companies] ([Id], [Name])
VALUES (@p0, @p1);
    },
    {
      Type: ReaderExecutedAsync,
      HasTransaction: false,
      Text:
SELECT [c].[Id], [c].[Name]
FROM [Companies] AS [c]
WHERE [c].[Name] = N'Title'
    }
  ]
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.MultiDbContexts.verified.txt#L1-L25' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.MultiDbContexts.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Disabling Recording for an instance

<!-- snippet: RecordingDisableForInstance -->
<a id='snippet-RecordingDisableForInstance'></a>
```cs
var company = new Company
{
    Name = "Title"
};
data.Add(company);
await data.SaveChangesAsync();

Recording.Start();

await data
    .Companies
    .Where(_ => _.Name == "Title")
    .ToListAsync();
data.DisableRecording();
await data
    .Companies
    .Where(_ => _.Name == "Disabled")
    .ToListAsync();

await Verify();
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L510-L533' title='Snippet source file'>snippet source</a> | <a href='#snippet-RecordingDisableForInstance' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: CoreTests.RecordingDisabledTest.verified.txt -->
<a id='snippet-CoreTests.RecordingDisabledTest.verified.txt'></a>
```txt
{
  ef: {
    Type: ReaderExecutedAsync,
    HasTransaction: false,
    Text:
SELECT [c].[Id], [c].[Name]
FROM [Companies] AS [c]
WHERE [c].[Name] = N'Title'
  }
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.RecordingDisabledTest.verified.txt#L1-L10' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.RecordingDisabledTest.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## ChangeTracking

Added, deleted, and Modified entities can be verified by performing changes on a DbContext and then verifying the instance of ChangeTracking. This approach leverages the [EntityFramework ChangeTracker](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.changetracking.changetracker).


### Added entity

This test:

<!-- snippet: Added -->
<a id='snippet-Added'></a>
```cs
[Test]
public async Task Added()
{
    var options = DbContextOptions();

    await using var data = new SampleDbContext(options);
    var company = new Company
    {
        Name = "company name"
    };
    data.Add(company);
    await Verify(data.ChangeTracker);
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L87-L103' title='Snippet source file'>snippet source</a> | <a href='#snippet-Added' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Will result in the following verified file:

<!-- snippet: CoreTests.Added.verified.txt -->
<a id='snippet-CoreTests.Added.verified.txt'></a>
```txt
{
  Added: {
    Company: {
      Id: 0,
      Name: company name
    }
  }
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.Added.verified.txt#L1-L8' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.Added.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Deleted entity

This test:

<!-- snippet: Deleted -->
<a id='snippet-Deleted'></a>
```cs
[Test]
public async Task Deleted()
{
    var options = DbContextOptions();

    await using var data = new SampleDbContext(options);
    data.Add(new Company
    {
        Name = "company name"
    });
    await data.SaveChangesAsync();

    var company = data.Companies.Single();
    data.Companies.Remove(company);
    await Verify(data.ChangeTracker);
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L105-L124' title='Snippet source file'>snippet source</a> | <a href='#snippet-Deleted' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Will result in the following verified file:

<!-- snippet: CoreTests.Deleted.verified.txt -->
<a id='snippet-CoreTests.Deleted.verified.txt'></a>
```txt
{
  Deleted: {
    Company: {
      Id: 0
    }
  }
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.Deleted.verified.txt#L1-L7' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.Deleted.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Modified entity

This test:

<!-- snippet: Modified -->
<a id='snippet-Modified'></a>
```cs
[Test]
public async Task Modified()
{
    var options = DbContextOptions();

    await using var data = new SampleDbContext(options);
    var company = new Company
    {
        Name = "old name"
    };
    data.Add(company);
    await data.SaveChangesAsync();

    data.Companies.Single()
        .Name = "new name";
    await Verify(data.ChangeTracker);
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L126-L146' title='Snippet source file'>snippet source</a> | <a href='#snippet-Modified' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Will result in the following verified file:

<!-- snippet: CoreTests.Modified.verified.txt -->
<a id='snippet-CoreTests.Modified.verified.txt'></a>
```txt
{
  Modified: {
    Company: {
      Id: 0,
      Name: {
        Original: old name,
        Current: new name
      }
    }
  }
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.Modified.verified.txt#L1-L11' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.Modified.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Queryable

This test:

<!-- snippet: Queryable -->
<a id='snippet-Queryable'></a>
```cs
var queryable = data.Companies
    .Where(_ => _.Name == "company name");
await Verify(queryable);
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L341-L347' title='Snippet source file'>snippet source</a> | <a href='#snippet-Queryable' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Will result in the following verified files:


### EF Core


#### CoreTests.Queryable.verified.txt

<!-- snippet: CoreTests.Queryable.verified.txt -->
<a id='snippet-CoreTests.Queryable.verified.txt'></a>
```txt
[
  {
    Name: company name
  }
]
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.Queryable.verified.txt#L1-L5' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.Queryable.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### CoreTests.Queryable.verified.sql

<!-- snippet: CoreTests.Queryable.verified.sql -->
<a id='snippet-CoreTests.Queryable.verified.sql'></a>
```sql
SELECT [c].[Id], [c].[Name]
FROM [Companies] AS [c]
WHERE [c].[Name] = N'company name'
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.Queryable.verified.sql#L1-L3' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.Queryable.verified.sql' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### EF Classic


#### ClassicTests.Queryable.verified.txt

<!-- snippet: ClassicTests.Queryable.verified.txt -->
<a id='snippet-ClassicTests.Queryable.verified.txt'></a>
```txt
SELECT 
    [Extent1].[Id] AS [Id], 
    [Extent1].[Content] AS [Content]
    FROM [dbo].[Companies] AS [Extent1]
    WHERE N'value' = [Extent1].[Content]
```
<sup><a href='/src/Verify.EntityFrameworkClassic.Tests/ClassicTests.Queryable.verified.txt#L1-L5' title='Snippet source file'>snippet source</a> | <a href='#snippet-ClassicTests.Queryable.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## AllData

This test:

<!-- snippet: AllData -->
<a id='snippet-AllData'></a>
```cs
await Verify(data.AllData())
    .AddExtraSettings(
        serializer =>
            serializer.TypeNameHandling = TypeNameHandling.Objects);
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L320-L327' title='Snippet source file'>snippet source</a> | <a href='#snippet-AllData' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Will result in the following verified file with all data in the database:

<!-- snippet: CoreTests.AllData.verified.txt -->
<a id='snippet-CoreTests.AllData.verified.txt'></a>
```txt
[
  {
    $type: Company,
    Id: 1,
    Name: Company1
  },
  {
    $type: Company,
    Id: 4,
    Name: Company2
  },
  {
    $type: Company,
    Id: 6,
    Name: Company3
  },
  {
    $type: Company,
    Id: 7,
    Name: Company4
  },
  {
    $type: Employee,
    Id: 2,
    CompanyId: 1,
    Name: Employee1,
    Age: 25
  },
  {
    $type: Employee,
    Id: 3,
    CompanyId: 1,
    Name: Employee2,
    Age: 31
  },
  {
    $type: Employee,
    Id: 5,
    CompanyId: 4,
    Name: Employee4,
    Age: 34
  }
]
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.AllData.verified.txt#L1-L43' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.AllData.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## IgnoreNavigationProperties

`IgnoreNavigationProperties` extends `SerializationSettings` to exclude all navigation properties from serialization:

<!-- snippet: IgnoreNavigationProperties -->
<a id='snippet-IgnoreNavigationProperties'></a>
```cs
[Test]
public async Task IgnoreNavigationProperties()
{
    var options = DbContextOptions();

    await using var data = new SampleDbContext(options);

    var company = new Company
    {
        Name = "company"
    };
    var employee = new Employee
    {
        Name = "employee",
        Company = company
    };
    await Verify(employee)
        .IgnoreNavigationProperties();
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L148-L170' title='Snippet source file'>snippet source</a> | <a href='#snippet-IgnoreNavigationProperties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Ignore globally

<!-- snippet: IgnoreNavigationPropertiesGlobal -->
<a id='snippet-IgnoreNavigationPropertiesGlobal'></a>
```cs
var options = DbContextOptions();
using var data = new SampleDbContext(options);
VerifyEntityFramework.IgnoreNavigationProperties();
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L198-L204' title='Snippet source file'>snippet source</a> | <a href='#snippet-IgnoreNavigationPropertiesGlobal' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## WebApplicationFactory

To be able to use [WebApplicationFactory](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1) for [integration testing](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests) an identifier must be used to be able to retrieve the recorded commands. Start by enable recording with a unique identifier, for example the test name or a GUID:

<!-- snippet: EnableRecordingWithIdentifier -->
<a id='snippet-EnableRecordingWithIdentifier'></a>
```cs
protected override void ConfigureWebHost(IWebHostBuilder webBuilder)
{
    var dataBuilder = new DbContextOptionsBuilder<SampleDbContext>()
        .EnableRecording(name)
        .UseSqlite($"Data Source={name};Mode=Memory;Cache=Shared");
    webBuilder.ConfigureTestServices(
        _ => _.AddScoped(
            _ => dataBuilder.Options));
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L593-L605' title='Snippet source file'>snippet source</a> | <a href='#snippet-EnableRecordingWithIdentifier' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Then use the same identifier for recording:

<!-- snippet: RecordWithIdentifier -->
<a id='snippet-RecordWithIdentifier'></a>
```cs
var httpClient = factory.CreateClient();

Recording.Start(testName);

var companies = await httpClient.GetFromJsonAsync<Company[]>("/companies");

var entries = Recording.Stop(testName);
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L566-L576' title='Snippet source file'>snippet source</a> | <a href='#snippet-RecordWithIdentifier' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The results will not be automatically included in verified file so it will have to be verified manually:

<!-- snippet: VerifyRecordedCommandsWithIdentifier -->
<a id='snippet-VerifyRecordedCommandsWithIdentifier'></a>
```cs
await Verify(
    new
    {
        target = companies!.Length,
        sql = entries
    });
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L578-L587' title='Snippet source file'>snippet source</a> | <a href='#snippet-VerifyRecordedCommandsWithIdentifier' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## ScrubInlineEfDateTimes

In some scenarios EntityFrmaeowrk does not parameterise DateTimes. For example when querying [temporal tables](https://learn.microsoft.com/en-us/sql/relational-databases/tables/temporal-tables).

`ScrubInlineEfDateTimes()` is a convenience method that calls `.ScrubInlineDateTimes("yyyy-MM-ddTHH:mm:ss.fffffffZ")`.


### Static usage

```
VerifyEntityFramework.ScrubInlineEfDateTimes();
```


### Instance usage

<!-- snippet: ScrubInlineEfDateTimesInstance -->
<a id='snippet-ScrubInlineEfDateTimesInstance'></a>
```cs
var settings = new VerifySettings();
settings.ScrubInlineEfDateTimes();
await Verify(target, settings);
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L34-L40' title='Snippet source file'>snippet source</a> | <a href='#snippet-ScrubInlineEfDateTimesInstance' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Fluent usage

<!-- snippet: ScrubInlineEfDateTimesFluent -->
<a id='snippet-ScrubInlineEfDateTimesFluent'></a>
```cs
await Verify(target)
    .ScrubInlineEfDateTimes();
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L48-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-ScrubInlineEfDateTimesFluent' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Icon

[Database](https://thenounproject.com/term/database/310841/) designed by [Creative Stall](https://thenounproject.com/creativestall/) from [The Noun Project](https://thenounproject.com).
