# <img src="/src/icon.png" height="30px"> Verify.EntityFramework

[![Discussions](https://img.shields.io/badge/Verify-Discussions-yellow?svg=true&label=)](https://github.com/orgs/VerifyTests/discussions)
[![Build status](https://github.com/VerifyTests/Verify.EntityFramework/actions/workflows/build.yml/badge.svg)](https://github.com/VerifyTests/Verify.EntityFramework/actions/workflows/build.yml)
[![NuGet Status](https://img.shields.io/nuget/v/Verify.EntityFramework.svg?label=Verify.EntityFramework)](https://www.nuget.org/packages/Verify.EntityFramework/)
[![NuGet Status](https://img.shields.io/nuget/v/Verify.EntityFrameworkClassic.svg?label=Verify.EntityFrameworkClassic)](https://www.nuget.org/packages/Verify.EntityFrameworkClassic/)

Extends [Verify](https://github.com/VerifyTests/Verify) to allow snapshot testing with EntityFramework.<!-- singleLineInclude: intro. path: /docs/intro.include.md -->

**See [Milestones](../../milestones?state=closed) for release notes.**


## Sponsors


### Entity Framework Extensions<!-- include: sponsors. path: /docs/sponsors.include.md -->

[Entity Framework Extensions](https://entityframework-extensions.net/?utm_source=simoncropp&utm_medium=Verify.EntityFramework) is a major sponsor and is proud to contribute to the development this project.

[![Entity Framework Extensions](https://raw.githubusercontent.com/VerifyTests/Verify.EntityFramework/refs/heads/main/docs/zzz.png)](https://entityframework-extensions.net/?utm_source=simoncropp&utm_medium=Verify.EntityFramework)

### Developed using JetBrains IDEs

[![JetBrains logo.](https://raw.githubusercontent.com/VerifyTests/Verify.EntityFramework/main/docs/jetbrains.png)](https://jb.gg/OpenSourceSupport)<!-- endInclude -->


## NuGet

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

Call `EnableRecording()` on `DbContextOptionsBuilder`.

<!-- snippet: EnableRecording -->
<a id='snippet-EnableRecording'></a>
```cs
var builder = new DbContextOptionsBuilder<SampleDbContext>();
builder.UseSqlServer(connection);
builder.EnableRecording();
var data = new SampleDbContext(builder.Options);
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L452-L459' title='Snippet source file'>snippet source</a> | <a href='#snippet-EnableRecording' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`EnableRecording` should only be called in the test context.


### Usage

To start recording call `Recording.Start()`. The results will be automatically included in verified file.

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
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L551-L569' title='Snippet source file'>snippet source</a> | <a href='#snippet-Recording' title='Start of snippet'>anchor</a></sup>
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
select c.Id,
       c.Name
from   Companies as c
where  c.Name = N'Title'
  }
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.RecordingTest.verified.txt#L1-L11' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.RecordingTest.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


Sql entries can be explicitly read using `Recording.Stop()`, optionally filtered, and passed to Verify:

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
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L773-L798' title='Snippet source file'>snippet source</a> | <a href='#snippet-RecordingSpecific' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### DbContext spanning

`Recording.Start()` can be called on different DbContext instances (built from the same options) and the results will be aggregated.

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
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L519-L542' title='Snippet source file'>snippet source</a> | <a href='#snippet-MultiDbContexts' title='Start of snippet'>anchor</a></sup>
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
set implicit_transactions off;

set nocount on;

insert  into Companies (Id, Name)
values                 (@p0, @p1)
    },
    {
      Type: ReaderExecutedAsync,
      HasTransaction: false,
      Text:
select c.Id,
       c.Name
from   Companies as c
where  c.Name = N'Title'
    }
  ]
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.MultiDbContexts.verified.txt#L1-L28' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.MultiDbContexts.verified.txt' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L647-L670' title='Snippet source file'>snippet source</a> | <a href='#snippet-RecordingDisableForInstance' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: CoreTests.RecordingDisabledTest.verified.txt -->
<a id='snippet-CoreTests.RecordingDisabledTest.verified.txt'></a>
```txt
{
  ef: {
    Type: ReaderExecutedAsync,
    HasTransaction: false,
    Text:
select c.Id,
       c.Name
from   Companies as c
where  c.Name = N'Title'
  }
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.RecordingDisabledTest.verified.txt#L1-L11' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.RecordingDisabledTest.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Disabling Recording globally

Recording is attached by `EnableRecording()`. Pass `recordCommands: false` to `Initialize` to leave that interceptor unattached, which makes every subsequent `EnableRecording()` call a no-op:

<!-- snippet: DisableRecording -->
<a id='snippet-DisableRecording'></a>
```cs
VerifyEntityFramework.Initialize(data, recordCommands: false);
```
<sup><a href='/src/Verify.EntityFramework.RecordingDisabledTests/ModuleInitializer.cs#L8-L12' title='Snippet source file'>snippet source</a> | <a href='#snippet-DisableRecording' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Only recording is disabled. The converters, and the queryable to SQL file converter, are still registered.

This is useful when another package records the same commands. [Verify.SqlServer](https://github.com/VerifyTests/Verify.SqlServer) subscribes to the `Microsoft.Data.SqlClient` diagnostic listener and records under the name `sql`. Since EF Core executes its commands through `SqlCommand`, with both packages recording every command EF executes is captured twice: once as `ef` and once as `sql`. Disable one of the two:

 * `VerifyEntityFramework.Initialize(model, recordCommands: false)` keeps the `sql` entries.
 * `VerifySqlServer.Initialize(recordCommands: false)` keeps the `ef` entries, which also carry the command `Type` and transaction state. It has to be called before `VerifierSettings.InitializePlugins()`, otherwise plugin discovery initializes Verify.SqlServer first with recording enabled, and the explicit call throws `Already Initialized`.


### InMemory

The [InMemory provider](https://learn.microsoft.com/en-us/ef/core/providers/in-memory/) executes no SQL, so there are no commands to record. Instead `EnableRecording()` records:

 * Each query, as the LINQ expression that EF executes, with captured variables extracted to `Parameters`.
 * Each `SaveChanges`, as the entities being added, modified, and deleted, in the same format as [ChangeTracking](#changetracking).

<!-- snippet: EnableRecordingInMemory -->
<a id='snippet-EnableRecordingInMemory'></a>
```cs
var builder = new DbContextOptionsBuilder<SampleDbContext>();
builder.UseInMemoryDatabase(databaseName);
builder.EnableRecording();
var data = new SampleDbContext(builder.Options);
```
<sup><a href='/src/Verify.EntityFramework.Tests/InMemoryRecordingTests.cs#L8-L15' title='Snippet source file'>snippet source</a> | <a href='#snippet-EnableRecordingInMemory' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: RecordingInMemory -->
<a id='snippet-RecordingInMemory'></a>
```cs
Recording.Start();

data.Add(
    new Company
    {
        Id = 1,
        Name = "Title"
    });
await data.SaveChangesAsync();

await data
    .Companies
    .Where(_ => _.Name == "Title")
    .ToListAsync();

await Verify();
```
<sup><a href='/src/Verify.EntityFramework.Tests/InMemoryRecordingTests.cs#L23-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-RecordingInMemory' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Will result in the following verified file:

<!-- snippet: InMemoryRecordingTests.RecordingInMemory.verified.txt -->
<a id='snippet-InMemoryRecordingTests.RecordingInMemory.verified.txt'></a>
```txt
{
  ef: [
    {
      Type: SaveChangesAsync,
      Added: {
        Company: {
          Id: 1,
          Name: Title
        }
      }
    },
    {
      Type: QueryAsync,
      Text:
DbSet<Company>()
    .Where(_ => _.Name == "Title")
    }
  ]
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/InMemoryRecordingTests.RecordingInMemory.verified.txt#L1-L19' title='Snippet source file'>snippet source</a> | <a href='#snippet-InMemoryRecordingTests.RecordingInMemory.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Queries compiled with `EF.CompileQuery` or `EF.CompileAsyncQuery` are not recorded, since after being compiled they execute without passing through the query compiler.


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
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L100-L116' title='Snippet source file'>snippet source</a> | <a href='#snippet-Added' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L118-L137' title='Snippet source file'>snippet source</a> | <a href='#snippet-Deleted' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L139-L159' title='Snippet source file'>snippet source</a> | <a href='#snippet-Modified' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L391-L397' title='Snippet source file'>snippet source</a> | <a href='#snippet-Queryable' title='Start of snippet'>anchor</a></sup>
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
select c.Id,
       c.Name
from   Companies as c
where  c.Name = N'company name'
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.Queryable.verified.sql#L1-L4' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.Queryable.verified.sql' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L370-L377' title='Snippet source file'>snippet source</a> | <a href='#snippet-AllData' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L161-L183' title='Snippet source file'>snippet source</a> | <a href='#snippet-IgnoreNavigationProperties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Ignore globally

<!-- snippet: IgnoreNavigationPropertiesGlobal -->
<a id='snippet-IgnoreNavigationPropertiesGlobal'></a>
```cs
var options = DbContextOptions();
using var data = new SampleDbContext(options);
VerifyEntityFramework.IgnoreNavigationProperties();
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L211-L217' title='Snippet source file'>snippet source</a> | <a href='#snippet-IgnoreNavigationPropertiesGlobal' title='Start of snippet'>anchor</a></sup>
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
        .UseSqlServer(connectionString);
    webBuilder.ConfigureTestServices(
        _ => _.AddScoped(
            _ => dataBuilder.Options));
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L727-L739' title='Snippet source file'>snippet source</a> | <a href='#snippet-EnableRecordingWithIdentifier' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L700-L710' title='Snippet source file'>snippet source</a> | <a href='#snippet-RecordWithIdentifier' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L712-L721' title='Snippet source file'>snippet source</a> | <a href='#snippet-VerifyRecordedCommandsWithIdentifier' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Descriptive Table Aliases

By default EF generates single character table aliases in SQL (eg `c` for Companies, `e` for Employees). `UseDescriptiveTableAliases` replaces these with the full table name, making recorded and verified SQL easier to read.


### Enable

Call `UseDescriptiveTableAliases()` on `DbContextOptionsBuilder`.

```cs
var builder = new DbContextOptionsBuilder<SampleDbContext>();
builder.UseSqlServer(connection);
builder.UseDescriptiveTableAliases();
```


### Result

With descriptive aliases enabled, the generated SQL:

```sql
select   companies.Id,
         companies.Name,
         employees.Id,
         employees.Age,
         employees.CompanyId,
         employees.Name
from     Companies as companies
         left outer join
         Employees as employees
         on companies.Id = employees.CompanyId
order by companies.Name,
         companies.Id
```

Instead of the default:

```sql
select   c.Id,
         c.Name,
         e.Id,
         e.Age,
         e.CompanyId,
         e.Name
from     Companies as c
         left outer join
         Employees as e
         on c.Id = e.CompanyId
order by c.Name,
         c.Id
```


## Descriptive Parameter Names

By default EF generates generic parameter names in SQL (eg `@p0`, `@p1`). `UseDescriptiveParameterNames` replaces these with the column name, making recorded and verified SQL easier to read. When the same column name appears across multiple tables in a batch, subsequent occurrences are prefixed with the entity type name (eg `@Id` for the first table, `@EmployeeId` for the second).


### Enable

Call `UseDescriptiveParameterNames()` on `DbContextOptionsBuilder`.

```cs
var builder = new DbContextOptionsBuilder<SampleDbContext>();
builder.UseSqlServer(connection);
builder.UseDescriptiveParameterNames();
```


### Result

With descriptive parameter names enabled, an insert:

<!-- snippet: CoreTests.DescriptiveParameterNames.verified.txt -->
<a id='snippet-CoreTests.DescriptiveParameterNames.verified.txt'></a>
```txt
{
  ef: {
    Type: ReaderExecutedAsync,
    HasTransaction: false,
    Parameters: {
      @Id (Int32): 0,
      @Name (String): Title
    },
    Text:
set implicit_transactions off;

set nocount on;

insert  into Companies (Id, Name)
values                 (@Id, @Name)
  }
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.DescriptiveParameterNames.verified.txt#L1-L17' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.DescriptiveParameterNames.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Instead of the default:

```txt
Parameters: {
  @p0 (Int32): 0,
  @p1 (String): Title
},
Text:
insert  into Companies (Id, Name)
values                (@p0, @p1)
```


### Duplicate column names

When multiple tables in the same batch have columns with the same name, subsequent occurrences are prefixed with the entity type name:

<!-- snippet: CoreTests.DescriptiveParameterNamesDuplicate.verified.txt -->
<a id='snippet-CoreTests.DescriptiveParameterNamesDuplicate.verified.txt'></a>
```txt
{
  ef: {
    Type: ReaderExecutedAsync,
    HasTransaction: true,
    Parameters: {
      @Age (Int32): 25,
      @CompanyId (Int32): 100,
      @EmployeeId (Int32): 200,
      @EmployeeName (String): EmployeeName,
      @Id (Int32): 100,
      @Name (String): CompanyName
    },
    Text:
set nocount on;

insert  into Companies (Id, Name)
values                 (@Id, @Name);

insert  into Employees (Id, Age, CompanyId, Name)
values                 (@EmployeeId, @Age, @CompanyId, @EmployeeName)
  }
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.DescriptiveParameterNamesDuplicate.verified.txt#L1-L22' title='Snippet source file'>snippet source</a> | <a href='#snippet-CoreTests.DescriptiveParameterNamesDuplicate.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

If the entity-prefixed name itself collides with an existing column name (eg `Company` + `Id` = `CompanyId` which is already a column on `Employee`), a counter suffix is used as a fallback.


## Missing OrderBy

To detect and correct missing `OrderBy` clauses in EF queries, use [EntityFramework.OrderBy](https://github.com/SimonCropp/EntityFramework.OrderBy).


## Anti-patterns

Queries that contain an anti-pattern throw when they are compiled. This works with any provider, and also applies to `ToQueryString()`, so verifying a [Queryable](#queryable) also throws.

`EnableRecording()` enables this by default. For a context that does not use recording, use `ThrowOnAntiPatterns()`:

<!-- snippet: ThrowOnAntiPatterns -->
<a id='snippet-ThrowOnAntiPatterns'></a>
```cs
var builder = new DbContextOptionsBuilder<SampleDbContext>();
builder.UseSqlServer(connectionString);
builder.ThrowOnAntiPatterns();
var data = new SampleDbContext(builder.Options);
```
<sup><a href='/src/Verify.EntityFramework.Tests/AntiPatternTests.cs#L8-L15' title='Snippet source file'>snippet source</a> | <a href='#snippet-ThrowOnAntiPatterns' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A context that uses `UseInternalServiceProvider` is not checked, since EF does not apply extension services to that provider.

These checks find queries that are written wrong, whatever data they run against. To limit how large or expensive a query can be, for example the number of values in a `Contains` list, the number of rows, or the number of includes, use [EfQueryComplexity](https://github.com/SimonCropp/EfQueryComplexity).


### Opting out

For a single context:

<!-- snippet: EnableRecordingAllowAntiPatterns -->
<a id='snippet-EnableRecordingAllowAntiPatterns'></a>
```cs
var builder = new DbContextOptionsBuilder<SampleDbContext>();
builder.UseInMemoryDatabase(nameof(EnableRecordingOptOut));
builder.EnableRecording(throwOnAntiPatterns: false);
```
<sup><a href='/src/Verify.EntityFramework.Tests/AntiPatternTests.cs#L461-L467' title='Snippet source file'>snippet source</a> | <a href='#snippet-EnableRecordingAllowAntiPatterns' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

For all contexts, at assembly load time and before any context is built:

<!-- snippet: ThrowOnAntiPatternsByDefault -->
<a id='snippet-ThrowOnAntiPatternsByDefault'></a>
```cs
VerifyEntityFramework.ThrowOnAntiPatternsByDefault = false;
```
<sup><a href='/src/Verify.EntityFramework.StaticSettingsTests/StaticSettingsTests.cs#L24-L28' title='Snippet source file'>snippet source</a> | <a href='#snippet-ThrowOnAntiPatternsByDefault' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

To allow one of the [EF warnings](#ef-warnings), use `ConfigureWarnings`. See below.


### Ignored Include and tracking options

EF only applies `Include` and `ThenInclude` to the entities returned by a query, and only tracks those entities. When a query ends in a projection, or a scalar like `Count` or `Any`, that returns no entity, EF silently ignores `Include`, `ThenInclude`, `AsNoTracking`, `AsNoTrackingWithIdentityResolution`, and `AsTracking`. A projection already loads the related data it references, so the ignored operator only misleads the reader.

<!-- snippet: IgnoredInclude -->
<a id='snippet-IgnoredInclude'></a>
```cs
await ThrowsTask(() =>
        data.Companies
            .Include(_ => _.Employees)
            .Select(_ => new
            {
                _.Name,
                EmployeeCount = _.Employees.Count
            })
            .ToListAsync())
    .IgnoreStackTrace();
```
<sup><a href='/src/Verify.EntityFramework.Tests/AntiPatternTests.cs#L26-L39' title='Snippet source file'>snippet source</a> | <a href='#snippet-IgnoredInclude' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Throws:

<!-- snippet: AntiPatternTests.IncludeThenProjection.verified.txt -->
<a id='snippet-AntiPatternTests.IncludeThenProjection.verified.txt'></a>
```txt
{
  Type: Exception,
  Message: Include(_ => _.Employees) is ignored, since it is followed by Select, which returns no entity. EF only applies Include to entities returned by the query, and a projection already loads the related data it references. Remove it.
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/AntiPatternTests.IncludeThenProjection.verified.txt#L1-L4' title='Snippet source file'>snippet source</a> | <a href='#snippet-AntiPatternTests.IncludeThenProjection.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The operators are kept when an entity is returned, including inside a projection, for example `Select(_ => new { Company = _, _.Name })`.


### Ignored query splitting

`AsSplitQuery()` and `AsSingleQuery()` only change how collections are loaded, by a collection `Include` or a collection in a projection. On a query that loads no collection they do nothing. A single collection is enough for `AsSplitQuery()` to have an effect, since it then avoids repeating the parent columns for each child row.

<!-- snippet: IgnoredSplitQuery -->
<a id='snippet-IgnoredSplitQuery'></a>
```cs
await Throws(() =>
        data.Employees
            .Include(_ => _.Company)
            .AsSplitQuery()
            .ToQueryString())
    .IgnoreStackTrace();
```
<sup><a href='/src/Verify.EntityFramework.Tests/AntiPatternTests.cs#L481-L490' title='Snippet source file'>snippet source</a> | <a href='#snippet-IgnoredSplitQuery' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Throws:

<!-- snippet: AntiPatternTests.SplitQueryWithoutCollection.verified.txt -->
<a id='snippet-AntiPatternTests.SplitQueryWithoutCollection.verified.txt'></a>
```txt
{
  Type: Exception,
  Message: AsSplitQuery() is ignored, since the query loads no collection. Query splitting only changes how collection Includes and collections in a projection are loaded. Remove it.
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/AntiPatternTests.SplitQueryWithoutCollection.verified.txt#L1-L4' title='Snippet source file'>snippet source</a> | <a href='#snippet-AntiPatternTests.SplitQueryWithoutCollection.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Discarded OrderBy

An `OrderBy` replaces any earlier ordering, so the earlier ordering is discarded. `ThenBy` was usually intended. An ordering followed by a row limiting operator, like `Take` or `Skip`, is kept. Queries inside lambdas, for example in a projection, are also checked.

<!-- snippet: DiscardedOrderBy -->
<a id='snippet-DiscardedOrderBy'></a>
```cs
await ThrowsTask(() =>
        data.Companies
            .OrderBy(_ => _.Name)
            .OrderBy(_ => _.Id)
            .ToListAsync())
    .IgnoreStackTrace();
```
<sup><a href='/src/Verify.EntityFramework.Tests/AntiPatternTests.cs#L325-L334' title='Snippet source file'>snippet source</a> | <a href='#snippet-DiscardedOrderBy' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Throws:

<!-- snippet: AntiPatternTests.OrderByThenOrderBy.verified.txt -->
<a id='snippet-AntiPatternTests.OrderByThenOrderBy.verified.txt'></a>
```txt
{
  Type: Exception,
  Message: OrderBy(_ => _.Name) is discarded, since it is followed by OrderBy(_ => _.Id). Use ThenBy(_ => _.Id) to add a secondary ordering, or remove the first ordering.
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/AntiPatternTests.OrderByThenOrderBy.verified.txt#L1-L4' title='Snippet source file'>snippet source</a> | <a href='#snippet-AntiPatternTests.OrderByThenOrderBy.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Redundant null check

EF evaluates a member of a null navigation as null, and null compared to a non null constant is false. So in `_.Owner != null && _.Owner.Name == "owner"` the null check is redundant, and `_.Owner!.Name == "owner"` returns the same rows with simpler SQL.

The same applies to nullable scalars, like an `int?` or a `string`, including checks using `HasValue`:

<!-- snippet: RedundantNullCheckNullableScalar -->
<a id='snippet-RedundantNullCheckNullableScalar'></a>
```cs
await ThrowsTask(() =>
        data.Cars
            .Where(_ => _.OwnerId != null && _.OwnerId > 0)
            .ToListAsync())
    .IgnoreStackTrace();
```
<sup><a href='/src/Verify.EntityFramework.Tests/NullableNavigationTests.cs#L98-L106' title='Snippet source file'>snippet source</a> | <a href='#snippet-RedundantNullCheckNullableScalar' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: RedundantNullCheck -->
<a id='snippet-RedundantNullCheck'></a>
```cs
await ThrowsTask(() =>
        data.Cars
            .Where(_ => _.Owner != null && _.Owner.Name == "owner")
            .ToListAsync())
    .IgnoreStackTrace();
```
<sup><a href='/src/Verify.EntityFramework.Tests/NullableNavigationTests.cs#L46-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-RedundantNullCheck' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Throws:

<!-- snippet: NullableNavigationTests.RedundantNullCheck.verified.txt -->
<a id='snippet-NullableNavigationTests.RedundantNullCheck.verified.txt'></a>
```txt
{
  Type: Exception,
  Message: The null check `_.Owner != null` is redundant, since `_.Owner.Name == "owner"` is false when _.Owner is null. Remove the null check.
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/NullableNavigationTests.RedundantNullCheck.verified.txt#L1-L4' title='Snippet source file'>snippet source</a> | <a href='#snippet-NullableNavigationTests.RedundantNullCheck.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Only comparisons with a non null constant using `==`, `>`, `>=`, `<`, or `<=` are detected. With `!=`, or a value that can be null, a null navigation can match, so the null check changes the result and is kept.


### EF warnings

EF detects some anti-patterns itself, but only logs them. `ThrowOnAntiPatterns()` configures these to throw:

 * `RelationalEventId.MultipleCollectionIncludeWarning`: more than one collection `Include` in a single query, which multiplies the rows returned. Use `AsSplitQuery()`, or configure a query splitting behavior.
 * `CoreEventId.RowLimitingOperationWithoutOrderByWarning`: `Take` or `Skip` without `OrderBy`, which returns unpredictable rows.
 * `CoreEventId.FirstWithoutOrderByAndFilterWarning`: `First` without `OrderBy` or a filter.
 * `CoreEventId.DistinctAfterOrderByWithoutRowLimitingOperatorWarning`: `Distinct` after `OrderBy`, which erases the ordering.
 * `CoreEventId.PossibleUnintendedReferenceComparisonWarning`: entities compared by reference.
 * `CoreEventId.PossibleUnintendedCollectionNavigationNullComparisonWarning`: a collection navigation compared to null.
 * `RelationalEventId.QueryPossibleUnintendedUseOfEqualsWarning`: `Equals` between values of different types.
 * `CoreEventId.NavigationBaseIncludeIgnored`: an `Include` of a navigation that fix-up already populates.
 * `CoreEventId.LazyLoadOnDisposedContextWarning` and `CoreEventId.DetachedLazyLoadingWarning`: lazy loading that does nothing.

Some of these are only logged by relational providers.

To allow one, call `ConfigureWarnings` after `ThrowOnAntiPatterns()`:

<!-- snippet: AllowAntiPatternWarning -->
<a id='snippet-AllowAntiPatternWarning'></a>
```cs
var builder = new DbContextOptionsBuilder<SampleDbContext>();
builder.UseSqlServer(connectionString);
builder.ThrowOnAntiPatterns();
builder.ConfigureWarnings(_ =>
    _.Ignore(CoreEventId.RowLimitingOperationWithoutOrderByWarning));
```
<sup><a href='/src/Verify.EntityFramework.Tests/AntiPatternTests.cs#L427-L435' title='Snippet source file'>snippet source</a> | <a href='#snippet-AllowAntiPatternWarning' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L78-L84' title='Snippet source file'>snippet source</a> | <a href='#snippet-ScrubInlineEfDateTimesInstance' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Fluent usage

<!-- snippet: ScrubInlineEfDateTimesFluent -->
<a id='snippet-ScrubInlineEfDateTimesFluent'></a>
```cs
await Verify(target)
    .ScrubInlineEfDateTimes();
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L92-L97' title='Snippet source file'>snippet source</a> | <a href='#snippet-ScrubInlineEfDateTimesFluent' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## DisableSqlFormatting

By default SQL captured against SQL Server is reformatted via [SqlFormatter](https://github.com/SimonCropp/SqlFormatter) before being written to the snapshot. This applies to both [Recording](#recording) output and [Queryable](#queryable) `.sql` files.

Reformatting can be disabled globally:

<!-- snippet: DisableSqlFormatting -->
<a id='snippet-DisableSqlFormatting'></a>
```cs
VerifyEntityFramework.DisableSqlFormatting = true;
```
<sup><a href='/src/Verify.EntityFramework.StaticSettingsTests/StaticSettingsTests.cs#L45-L49' title='Snippet source file'>snippet source</a> | <a href='#snippet-DisableSqlFormatting' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

When disabled, the SQL is written verbatim as produced by EntityFramework.


## Replaying recent migrations

Migrations are usually only ever tested against a database built by migrating an empty one. Deployed databases are not like that: a deployment applies state *after* migrating, such as enabling change tracking, rebuilding views, or re-granting permissions. That state can make DDL that is valid against an empty database fail against a real one. SQL Server, for example, refuses to drop a primary key while change tracking is enabled on the table.

`ReplayRecentMigrations` applies the most recent migrations one at a time, running a callback after each, so migrations meet the conditions a deployment gives them.

The database has to start with no migrations applied, so build it from an empty template.

<!-- snippet: MigrationReplayInstance -->
<a id='snippet-MigrationReplayInstance'></a>
```cs
// the template is left empty, so each test migrates forward from nothing
static SqlInstance<MyDbContext> sqlInstance = new(
    constructInstance: builder => new(builder.Options),
    buildTemplate: _ => Task.CompletedTask);
```
<sup><a href='/src/Verify.EntityFramework.Tests/MigrationReplay/MigrationReplaySnippets.cs#L5-L12' title='Snippet source file'>snippet source</a> | <a href='#snippet-MigrationReplayInstance' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: MigrationReplayUsage -->
<a id='snippet-MigrationReplayUsage'></a>
```cs
await using var database = await sqlInstance.Build();

await database.Context.ReplayRecentMigrations(
    count: 5,
    afterEachMigration: ApplyDeploymentState);
```
<sup><a href='/src/Verify.EntityFramework.Tests/MigrationReplay/MigrationReplaySnippets.cs#L16-L24' title='Snippet source file'>snippet source</a> | <a href='#snippet-MigrationReplayUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The callback applies whatever the deployment applies after migrating.

<!-- snippet: MigrationReplayAfterEach -->
<a id='snippet-MigrationReplayAfterEach'></a>
```cs
// whatever the deployment does after migrating: enabling
// change tracking, rebuilding views, re-granting permissions
static Task ApplyDeploymentState(MyDbContext data) =>
    Task.CompletedTask;
```
<sup><a href='/src/Verify.EntityFramework.Tests/MigrationReplay/MigrationReplaySnippets.cs#L27-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-MigrationReplayAfterEach' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Everything before the window is applied in a single hop, since those migrations are not under test. From there each migration is applied on its own, with the callback in between.

The one at a time part is the point. Applying the window in a single hop and running the callback once at the end is not equivalent: a table created by a migration *inside* the window would never have the state applied to it before a later migration alters it, and that is exactly the case that tends to break.

Migrating a database that is already up to date would revert migrations by running their `Down`, so this throws rather than doing that.


## Icon

[Database](https://thenounproject.com/term/database/310841/) designed by [Creative Stall](https://thenounproject.com/creativestall/) from [The Noun Project](https://thenounproject.com).
