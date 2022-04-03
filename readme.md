# <img src="/src/icon.png" height="30px"> Verify.EntityFramework

[![Build status](https://ci.appveyor.com/api/projects/status/g6njwv0aox62atu0?svg=true)](https://ci.appveyor.com/project/SimonCropp/verify-entityframework)
[![NuGet Status](https://img.shields.io/nuget/v/Verify.EntityFramework.svg?label=Verify.EntityFramework)](https://www.nuget.org/packages/Verify.EntityFramework/)
[![NuGet Status](https://img.shields.io/nuget/v/Verify.EntityFrameworkClassic.svg?label=Verify.EntityFrameworkClassic)](https://www.nuget.org/packages/Verify.EntityFrameworkClassic/)

Extends [Verify](https://github.com/VerifyTests/Verify) to allow snapshot testing with EntityFramework.


## NuGet packages

 * https://nuget.org/packages/Verify.EntityFramework/
 * https://nuget.org/packages/Verify.EntityFrameworkClassic/


## Enable

Enable VerifyEntityFramework once at assembly load time:


### EF Core

<!-- snippet: EnableCore -->
<a id='snippet-enablecore'></a>
```cs
VerifyEntityFramework.Enable();
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L473-L477' title='Snippet source file'>snippet source</a> | <a href='#snippet-enablecore' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### EF Classic

<!-- snippet: EnableClassic -->
<a id='snippet-enableclassic'></a>
```cs
VerifyEntityFrameworkClassic.Enable();
```
<sup><a href='/src/Verify.EntityFrameworkClassic.Tests/ClassicTests.cs#L132-L136' title='Snippet source file'>snippet source</a> | <a href='#snippet-enableclassic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Recording

Recording allows all commands executed by EF to be captured and then (optionally) verified.


### Enable

Call `EfRecording.EnableRecording()` on `DbContextOptionsBuilder`.

<!-- snippet: EnableRecording -->
<a id='snippet-enablerecording'></a>
```cs
var builder = new DbContextOptionsBuilder<SampleDbContext>();
builder.UseSqlServer(connection);
builder.EnableRecording();
var data = new SampleDbContext(builder.Options);
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L235-L242' title='Snippet source file'>snippet source</a> | <a href='#snippet-enablerecording' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`EnableRecording` should only be called in the test context.


### Usage

To start recording call `EfRecording.StartRecording()`. The results will be automatically included in verified file.

<!-- snippet: Recording -->
<a id='snippet-recording'></a>
```cs
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
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L315-L332' title='Snippet source file'>snippet source</a> | <a href='#snippet-recording' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Will result in the following verified file:

<!-- snippet: CoreTests.Recording.verified.txt -->
<a id='snippet-CoreTests.Recording.verified.txt'></a>
```txt
{
  target: 5,
  sql: [
    {
      Type: ReaderExecutedAsync,
      Text:
SELECT [c].[Id], [c].[Content]
FROM [Companies] AS [c]
WHERE [c].[Content] = N'Title'
    },
    {
      Type: ReaderExecuted,
      Text:
SELECT COUNT(*)
FROM [Companies] AS [c]
    }
  ]
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.Recording.verified.txt#L1-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.Recording.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


Sql entries can be explicitly read using `EfRecording.FinishRecording`, optionally filtered, and passed to Verify:

<!-- snippet: RecordingSpecific -->
<a id='snippet-recordingspecific'></a>
```cs
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
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L439-L462' title='Snippet source file'>snippet source</a> | <a href='#snippet-recordingspecific' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### DbContext spanning

`StartRecording` can be called on different DbContext instances (built from the same options) and the results will be aggregated.

<!-- snippet: MultiDbContexts -->
<a id='snippet-multidbcontexts'></a>
```cs
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
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L284-L306' title='Snippet source file'>snippet source</a> | <a href='#snippet-multidbcontexts' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: CoreTests.MultiDbContexts.verified.txt -->
<a id='snippet-CoreTests.MultiDbContexts.verified.txt'></a>
```txt
{
  target: 5,
  sql: [
    {
      Type: ReaderExecutedAsync,
      HasTransaction: true,
      Parameters: {
        @p0 (Int32): 0,
        @p1 (String?): Title
      },
      Text:
SET NOCOUNT ON;
INSERT INTO [Companies] ([Id], [Content])
VALUES (@p0, @p1);
    },
    {
      Type: ReaderExecutedAsync,
      Text:
SELECT [c].[Id], [c].[Content]
FROM [Companies] AS [c]
WHERE [c].[Content] = N'Title'
    },
    {
      Type: ReaderExecuted,
      Text:
SELECT COUNT(*)
FROM [Companies] AS [c]
    }
  ]
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.MultiDbContexts.verified.txt#L1-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.MultiDbContexts.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## ChangeTracking

Added, deleted, and Modified entities can be verified by performing changes on a DbContext and then verifying the instance of ChangeTracking. This approach leverages the [EntityFramework ChangeTracker](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.changetracking.changetracker).


### Added entity

This test:

<!-- snippet: Added -->
<a id='snippet-added'></a>
```cs
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
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L17-L33' title='Snippet source file'>snippet source</a> | <a href='#snippet-added' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Will result in the following verified file:

<!-- snippet: CoreTests.Added.verified.txt -->
<a id='snippet-CoreTests.Added.verified.txt'></a>
```txt
{
  Added: {
    Company: {
      Id: 0,
      Content: before
    }
  }
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.Added.verified.txt#L1-L8' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.Added.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Deleted entity

This test:

<!-- snippet: Deleted -->
<a id='snippet-deleted'></a>
```cs
[Test]
public async Task Deleted()
{
    var options = DbContextOptions();

    await using var data = new SampleDbContext(options);
    data.Add(new Company
    {
        Content = "before"
    });
    await data.SaveChangesAsync();

    var company = data.Companies.Single();
    data.Companies.Remove(company);
    await Verify(data.ChangeTracker);
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L35-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-deleted' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-modified'></a>
```cs
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
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L56-L75' title='Snippet source file'>snippet source</a> | <a href='#snippet-modified' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Will result in the following verified file:

<!-- snippet: CoreTests.Modified.verified.txt -->
<a id='snippet-CoreTests.Modified.verified.txt'></a>
```txt
{
  Modified: {
    Company: {
      Id: 0,
      Content: {
        Original: before,
        Current: after
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
<a id='snippet-queryable'></a>
```cs
var queryable = data.Companies
    .Where(x => x.Content == "value");
await Verify(queryable);
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L214-L220' title='Snippet source file'>snippet source</a> | <a href='#snippet-queryable' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Will result in the following verified file:


### EF Core

<!-- snippet: CoreTests.Queryable.verified.txt -->
<a id='snippet-CoreTests.Queryable.verified.txt'></a>
```txt
SELECT [c].[Id], [c].[Content]
FROM [Companies] AS [c]
WHERE [c].[Content] = N'value'
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.Queryable.verified.txt#L1-L3' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.Queryable.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### EF Classic

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
<a id='snippet-alldata'></a>
```cs
await Verify(data.AllData())
    .ModifySerialization(
        serialization =>
            serialization.AddExtraSettings(
                serializer =>
                    serializer.TypeNameHandling = TypeNameHandling.Objects));
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L196-L205' title='Snippet source file'>snippet source</a> | <a href='#snippet-alldata' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Will result in the following verified file with all data in the database:

<!-- snippet: CoreTests.AllData.verified.txt -->
<a id='snippet-CoreTests.AllData.verified.txt'></a>
```txt
[
  {
    $type: Company,
    Id: Id_1,
    Content: Company1
  },
  {
    $type: Company,
    Id: Id_2,
    Content: Company2
  },
  {
    $type: Company,
    Id: Id_3,
    Content: Company3
  },
  {
    $type: Company,
    Id: Id_4,
    Content: Company4
  },
  {
    $type: Employee,
    Id: Id_5,
    CompanyId: Id_1,
    Content: Employee1,
    Age: 25
  },
  {
    $type: Employee,
    Id: Id_6,
    CompanyId: Id_1,
    Content: Employee2,
    Age: 31
  },
  {
    $type: Employee,
    Id: Id_7,
    CompanyId: Id_2,
    Content: Employee4,
    Age: 34
  }
]
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.AllData.verified.txt#L1-L43' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.AllData.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## IgnoreNavigationProperties

`IgnoreNavigationProperties` extends `SerializationSettings` to exclude all navigation properties from serialization:

<!-- snippet: IgnoreNavigationProperties -->
<a id='snippet-ignorenavigationproperties'></a>
```cs
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
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L77-L100' title='Snippet source file'>snippet source</a> | <a href='#snippet-ignorenavigationproperties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## WebApplicationFactory

To be able to use [WebApplicationFactory](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1) for [integration testing](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)an identifier must be used to be able to retrieve the recorded commands. Start by enable recording with a unique identifier, for example the test name or a GUID:

<!-- snippet: EnableRecordingWithIdentifier -->
<a id='snippet-enablerecordingwithidentifier'></a>
```cs
.ConfigureTestServices(services =>
{
    services.AddScoped(_ =>
        new DbContextOptionsBuilder<SampleDbContext>()
            .EnableRecording(testName)
            .UseSqlite($"Data Source={testName};Mode=Memory;Cache=Shared")
            .Options);
});
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L397-L408' title='Snippet source file'>snippet source</a> | <a href='#snippet-enablerecordingwithidentifier' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Then use the same identifier for recording:

<!-- snippet: RecordWithIdentifier -->
<a id='snippet-recordwithidentifier'></a>
```cs
var httpClient = factory.CreateClient();

EfRecording.StartRecording(testName);

var companies = await httpClient.GetFromJsonAsync<Company[]>("/companies");

var entries = EfRecording.FinishRecording(testName);
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L364-L374' title='Snippet source file'>snippet source</a> | <a href='#snippet-recordwithidentifier' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The results will not be automatically included in verified file so it will have to be verified manually:

<!-- snippet: VerifyRecordedCommandsWithIdentifier -->
<a id='snippet-verifyrecordedcommandswithidentifier'></a>
```cs
await Verify(new
{
    target = companies!.Length,
    sql = entries
});
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L376-L384' title='Snippet source file'>snippet source</a> | <a href='#snippet-verifyrecordedcommandswithidentifier' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Icon

[Database](https://thenounproject.com/term/database/310841/) designed by [Creative Stall](https://thenounproject.com/creativestall/) from [The Noun Project](https://thenounproject.com).
