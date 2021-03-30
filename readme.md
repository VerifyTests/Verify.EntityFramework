# <img src="/src/icon.png" height="30px"> Verify.EntityFramework

[![Build status](https://ci.appveyor.com/api/projects/status/g6njwv0aox62atu0?svg=true)](https://ci.appveyor.com/project/SimonCropp/verify-entityframework)
[![NuGet Status](https://img.shields.io/nuget/v/Verify.EntityFramework.svg)](https://www.nuget.org/packages/Verify.EntityFramework/)
[![NuGet Status](https://img.shields.io/nuget/v/Verify.EntityFrameworkClassic.svg)](https://www.nuget.org/packages/Verify.EntityFrameworkClassic/)

Extends [Verify](https://github.com/VerifyTests/Verify) to allow snapshot testing with EntityFramework.

Support is available via a [Tidelift Subscription](https://tidelift.com/subscription/pkg/nuget-verify?utm_source=nuget-verify&utm_medium=referral&utm_campaign=enterprise).

<a href='https://dotnetfoundation.org' alt='Part of the .NET Foundation'><img src='https://raw.githubusercontent.com/VerifyTests/Verify/master/docs/dotNetFoundation.svg' height='30px'></a><br>
Part of the <a href='https://dotnetfoundation.org' alt=''>.NET Foundation</a>

<!-- toc -->
## Contents

  * [Enable](#enable)
    * [EF Core](#ef-core)
    * [EF Classic](#ef-classic)
  * [Recording](#recording)
    * [Enable](#enable-1)
    * [Usage](#usage)
    * [DbContext spanning](#dbcontext-spanning)
  * [ChangeTracking](#changetracking)
    * [Added entity](#added-entity)
    * [Deleted entity](#deleted-entity)
    * [Modified entity](#modified-entity)
  * [Queryable](#queryable)
    * [EF Core](#ef-core-1)
    * [EF Classic](#ef-classic-1)
  * [AllData](#alldata)
  * [IgnoreNavigationProperties](#ignorenavigationproperties)
  * [Security contact information](#security-contact-information)<!-- endToc -->


## NuGet package

 * https://nuget.org/packages/Verify.EntityFramework/
 * https://nuget.org/packages/Verify.EntityFrameworkClassic/


## Enable

Enable VerifyEntityFramewok once at assembly load time:


### EF Core

<!-- snippet: EnableCore -->
<a id='snippet-enablecore'></a>
```cs
VerifyEntityFramework.Enable();
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L344-L348' title='Snippet source file'>snippet source</a> | <a href='#snippet-enablecore' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### EF Classic

<!-- snippet: EnableClassic -->
<a id='snippet-enableclassic'></a>
```cs
VerifyEntityFrameworkClassic.Enable();
```
<sup><a href='/src/Verify.EntityFrameworkClassic.Tests/ClassicTests.cs#L137-L141' title='Snippet source file'>snippet source</a> | <a href='#snippet-enableclassic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Recording

Recording allows all commands executed by EF to be captured and then (optionally) verified.


### Enable

Call `SqlRecording.EnableRecording()` on `DbContextOptionsBuilder`.

<!-- snippet: EnableRecording -->
<a id='snippet-enablerecording'></a>
```cs
DbContextOptionsBuilder<SampleDbContext> builder = new();
builder.UseSqlServer(connection);
builder.EnableRecording();
SampleDbContext data = new(builder.Options);
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L202-L209' title='Snippet source file'>snippet source</a> | <a href='#snippet-enablerecording' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`EnableRecording` should only be called in the test context.


### Usage

To start recording call `SqlRecording.StartRecording()`. The results will be automatically included in verified file.

<!-- snippet: Recording -->
<a id='snippet-recording'></a>
```cs
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
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L282-L299' title='Snippet source file'>snippet source</a> | <a href='#snippet-recording' title='Start of snippet'>anchor</a></sup>
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


Sql entries can be explicitly read using `SqlRecording.FinishRecording`, optionally filtered, and passed to Verify:

<!-- snippet: RecordingSpecific -->
<a id='snippet-recordingspecific'></a>
```cs
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
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L308-L331' title='Snippet source file'>snippet source</a> | <a href='#snippet-recordingspecific' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### DbContext spanning

`StartRecording` can be called on different DbContext instances (built from the same options) and the results will be aggregated.

<!-- snippet: MultiDbContexts -->
<a id='snippet-multidbcontexts'></a>
```cs
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
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L251-L273' title='Snippet source file'>snippet source</a> | <a href='#snippet-multidbcontexts' title='Start of snippet'>anchor</a></sup>
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

    await using SampleDbContext data = new(options);
    Company company = new()
    {
        Content = "before"
    };
    data.Add(company);
    await Verifier.Verify(data.ChangeTracker);
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L14-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-added' title='Start of snippet'>anchor</a></sup>
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

    await using SampleDbContext data = new(options);
    data.Add(new Company {Content = "before"});
    await data.SaveChangesAsync();

    var company = data.Companies.Single();
    data.Companies.Remove(company);
    await Verifier.Verify(data.ChangeTracker);
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L32-L48' title='Snippet source file'>snippet source</a> | <a href='#snippet-deleted' title='Start of snippet'>anchor</a></sup>
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
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L50-L69' title='Snippet source file'>snippet source</a> | <a href='#snippet-modified' title='Start of snippet'>anchor</a></sup>
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
await Verifier.Verify(queryable);
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L180-L186' title='Snippet source file'>snippet source</a> | <a href='#snippet-queryable' title='Start of snippet'>anchor</a></sup>
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
await Verifier.Verify(data.AllData())
    .ModifySerialization(
        serialization =>
            serialization.AddExtraSettings(
                serializer =>
                    serializer.TypeNameHandling = TypeNameHandling.Objects));
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L161-L170' title='Snippet source file'>snippet source</a> | <a href='#snippet-alldata' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Will result in the following verified file with all data in the database:

<!-- snippet: CoreTests.AllData.verified.txt -->
<a id='snippet-CoreTests.AllData.verified.txt'></a>
```txt
[
  {
    $type: Company,
    Id: 1,
    Content: Company1
  },
  {
    $type: Company,
    Id: 4,
    Content: Company2
  },
  {
    $type: Company,
    Id: 6,
    Content: Company3
  },
  {
    $type: Company,
    Id: 7,
    Content: Company4
  },
  {
    $type: Employee,
    Id: 2,
    CompanyId: 1,
    Content: Employee1,
    Age: 25
  },
  {
    $type: Employee,
    Id: 3,
    CompanyId: 1,
    Content: Employee2,
    Age: 31
  },
  {
    $type: Employee,
    Id: 5,
    CompanyId: 4,
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

    await using SampleDbContext data = new(options);

    Company company = new()
    {
        Content = "company"
    };
    Employee employee = new()
    {
        Content = "employee",
        Company = company
    };
    await Verifier.Verify(employee)
        .ModifySerialization(
            x => x.IgnoreNavigationProperties(data));
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L71-L94' title='Snippet source file'>snippet source</a> | <a href='#snippet-ignorenavigationproperties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Security contact information

To report a security vulnerability, use the [Tidelift security contact](https://tidelift.com/security). Tidelift will coordinate the fix and disclosure.


## Icon

[Database](https://thenounproject.com/term/database/310841/) designed by [Creative Stall](https://thenounproject.com/creativestall/) from [The Noun Project](https://thenounproject.com/creativepriyanka).
