# <img src="/src/icon.png" height="30px"> Verify.EntityFramework

[![Build status](https://ci.appveyor.com/api/projects/status/g6njwv0aox62atu0?svg=true)](https://ci.appveyor.com/project/SimonCropp/verify-entityframework)
[![NuGet Status](https://img.shields.io/nuget/v/Verify.EntityFramework.svg)](https://www.nuget.org/packages/Verify.EntityFramework/)
[![NuGet Status](https://img.shields.io/nuget/v/Verify.EntityFrameworkClassic.svg)](https://www.nuget.org/packages/Verify.EntityFrameworkClassic/)

Extends [Verify](https://github.com/VerifyTests/Verify) to allow verification of EntityFramework bits.

Support is available via a [Tidelift Subscription](https://tidelift.com/subscription/pkg/nuget-verify?utm_source=nuget-verify&utm_medium=referral&utm_campaign=enterprise).

<a href='https://dotnetfoundation.org' alt='Part of the .NET Foundation'><img src='https://raw.githubusercontent.com/VerifyTests/Verify/master/docs/dotNetFoundation.svg' height='30px'></a><br>
Part of the <a href='https://dotnetfoundation.org' alt=''>.NET Foundation</a>

<!-- toc -->
## Contents

  * [Usage](#usage)
    * [EF Core](#ef-core)
    * [EF Classic](#ef-classic)
    * [Recording](#recording)
    * [ChangeTracking](#changetracking)
    * [Queryable](#queryable)
    * [EF Core](#ef-core-1)
    * [EF Classic](#ef-classic-1)
  * [Security contact information](#security-contact-information)<!-- endToc -->


## NuGet package

 * https://nuget.org/packages/Verify.EntityFramework/
 * https://nuget.org/packages/Verify.EntityFrameworkClassic/


## Usage

Enable VerifyEntityFramewok once at assembly load time:


### EF Core

<!-- snippet: EnableCore -->
<a id='enablecore'></a>
```cs
VerifyEntityFramework.Enable();
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L265-L269' title='Snippet source file'>snippet source</a> | <a href='#enablecore' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### EF Classic

<!-- snippet: EnableClassic -->
<a id='enableclassic'></a>
```cs
VerifyEntityFrameworkClassic.Enable();
```
<sup><a href='/src/Verify.EntityFrameworkClassic.Tests/ClassicTests.cs#L139-L143' title='Snippet source file'>snippet source</a> | <a href='#enableclassic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Recording

Recording allows all commands executed by EF to be captured and then (optionally) verified.


#### Enable

Call `SqlRecording.EnableRecording()` on `DbContextOptionsBuilder`.

<!-- snippet: EnableRecording -->
<a id='enablerecording'></a>
```cs
var builder = new DbContextOptionsBuilder<SampleDbContext>();
builder.UseSqlServer(connection);
builder.EnableRecording();
var data = new SampleDbContext(builder.Options);
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L155-L162' title='Snippet source file'>snippet source</a> | <a href='#enablerecording' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`EnableRecording` should only be called in the test context.


#### Usage

On the `DbContext` call `SqlRecording.StartRecording()` to start recording.

<!-- snippet: Recording -->
<a id='recording'></a>
```cs
[Test]
public async Task Recording()
{
    var database = await DbContextBuilder.GetDatabase("Recording");
    var data = database.Context;
    var company = new Company
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
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L228-L251' title='Snippet source file'>snippet source</a> | <a href='#recording' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Will result in the following verified file:

<!-- snippet: CoreTests.Recording.verified.txt -->
<a id='CoreTests.Recording.verified.txt'></a>
```txt
{
  target: '5',
  sql: [
    {
      Type: 'ReaderExecutedAsync',
      Text: "SELECT [c].[Id], [c].[Content]
FROM [Companies] AS [c]
WHERE [c].[Content] = N'Title'"
    },
    {
      Type: 'ReaderExecuted',
      Text: 'SELECT COUNT(*)
FROM [Companies] AS [c]'
    }
  ]
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.Recording.verified.txt#L1-L16' title='Snippet source file'>snippet source</a> | <a href='#CoreTests.Recording.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### DbContext spanning

`StartRecording` can be called on different DbContext instances (built from the same options) and the results will be aggregated.

<!-- snippet: MultiDbContexts -->
<a id='multidbcontexts'></a>
```cs
var builder = new DbContextOptionsBuilder<SampleDbContext>();
builder.UseSqlServer(connectionString);
builder.EnableRecording();

await using var data1 = new SampleDbContext(builder.Options);
SqlRecording.StartRecording();
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

await Verifier.Verify(data2.Companies.Count());
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L203-L225' title='Snippet source file'>snippet source</a> | <a href='#multidbcontexts' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: CoreTests.MultiDbContexts.verified.txt -->
<a id='CoreTests.MultiDbContexts.verified.txt'></a>
```txt
{
  target: '5',
  sql: [
    {
      Type: 'ReaderExecutedAsync',
      HasTransaction: true,
      Parameters: {
        @p0: 0,
        @p1: 'Title'
      },
      Text: 'SET NOCOUNT ON;
INSERT INTO [Companies] ([Id], [Content])
VALUES (@p0, @p1);'
    },
    {
      Type: 'ReaderExecutedAsync',
      Text: "SELECT [c].[Id], [c].[Content]
FROM [Companies] AS [c]
WHERE [c].[Content] = N'Title'"
    },
    {
      Type: 'ReaderExecuted',
      Text: 'SELECT COUNT(*)
FROM [Companies] AS [c]'
    }
  ]
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.MultiDbContexts.verified.txt#L1-L27' title='Snippet source file'>snippet source</a> | <a href='#CoreTests.MultiDbContexts.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### ChangeTracking

Added, deleted, and Modified entities can be verified by performing changes on a DbContext and then verifying the instance of ChangeTracking. This approach leverages the [EntityFramework ChangeTracker](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.changetracking.changetracker).


#### Added entity

This test:

<!-- snippet: Added -->
<a id='added'></a>
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
    await Verifier.Verify(data.ChangeTracker);
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L13-L29' title='Snippet source file'>snippet source</a> | <a href='#added' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Will result in the following verified file:

<!-- snippet: CoreTests.Added.verified.txt -->
<a id='CoreTests.Added.verified.txt'></a>
```txt
{
  Added: {
    Company: {
      Id: 0,
      Content: 'before'
    }
  }
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.Added.verified.txt#L1-L8' title='Snippet source file'>snippet source</a> | <a href='#CoreTests.Added.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Deleted entity

This test:

<!-- snippet: Deleted -->
<a id='deleted'></a>
```cs
[Test]
public async Task Deleted()
{
    var options = DbContextOptions();

    await using var data = new SampleDbContext(options);
    data.Add(new Company {Content = "before"});
    await data.SaveChangesAsync();

    var company = data.Companies.Single();
    data.Companies.Remove(company);
    await Verifier.Verify(data.ChangeTracker);
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L31-L47' title='Snippet source file'>snippet source</a> | <a href='#deleted' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Will result in the following verified file:

<!-- snippet: CoreTests.Deleted.verified.txt -->
<a id='CoreTests.Deleted.verified.txt'></a>
```txt
{
  Deleted: {
    Company: {
      Id: 0
    }
  }
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.Deleted.verified.txt#L1-L7' title='Snippet source file'>snippet source</a> | <a href='#CoreTests.Deleted.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Modified entity

This test:

<!-- snippet: Modified -->
<a id='modified'></a>
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
    await Verifier.Verify(data.ChangeTracker);
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L49-L68' title='Snippet source file'>snippet source</a> | <a href='#modified' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Will result in the following verified file:

<!-- snippet: CoreTests.Modified.verified.txt -->
<a id='CoreTests.Modified.verified.txt'></a>
```txt
{
  Modified: {
    Company: {
      Id: 0,
      Content: {
        Original: 'before',
        Current: 'after'
      }
    }
  }
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.Modified.verified.txt#L1-L11' title='Snippet source file'>snippet source</a> | <a href='#CoreTests.Modified.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Queryable

This test:

<!-- snippet: Queryable -->
<a id='queryable'></a>
```cs
[Test]
public async Task Queryable()
{
    var database = await DbContextBuilder.GetDatabase("Queryable");
    var data = database.Context;
    var queryable = data.Companies
        .Where(x => x.Content == "value");
    await Verifier.Verify(queryable);
}
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.cs#L129-L141' title='Snippet source file'>snippet source</a> | <a href='#queryable' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Will result in the following verified file:


### EF Core

<!-- snippet: CoreTests.Queryable.verified.txt -->
<a id='CoreTests.Queryable.verified.txt'></a>
```txt
SELECT [c].[Id], [c].[Content]
FROM [Companies] AS [c]
WHERE [c].[Content] = N'value'
```
<sup><a href='/src/Verify.EntityFramework.Tests/CoreTests.Queryable.verified.txt#L1-L3' title='Snippet source file'>snippet source</a> | <a href='#CoreTests.Queryable.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### EF Classic

<!-- snippet: ClassicTests.Queryable.verified.txt -->
<a id='ClassicTests.Queryable.verified.txt'></a>
```txt
SELECT 
    [Extent1].[Id] AS [Id], 
    [Extent1].[Content] AS [Content]
    FROM [dbo].[Companies] AS [Extent1]
    WHERE N'value' = [Extent1].[Content]
```
<sup><a href='/src/Verify.EntityFrameworkClassic.Tests/ClassicTests.Queryable.verified.txt#L1-L5' title='Snippet source file'>snippet source</a> | <a href='#ClassicTests.Queryable.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Security contact information

To report a security vulnerability, use the [Tidelift security contact](https://tidelift.com/security). Tidelift will coordinate the fix and disclosure.


## Icon

[Database](https://thenounproject.com/term/database/310841/) designed by [Creative Stall](https://thenounproject.com/creativestall/) from [The Noun Project](https://thenounproject.com/creativepriyanka).
