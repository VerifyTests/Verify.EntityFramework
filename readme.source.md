# <img src="/src/icon.png" height="30px"> Verify.EntityFramework

[![Build status](https://ci.appveyor.com/api/projects/status/g6njwv0aox62atu0?svg=true)](https://ci.appveyor.com/project/SimonCropp/verify-entityframework)
[![NuGet Status](https://img.shields.io/nuget/v/Verify.EntityFramework.svg)](https://www.nuget.org/packages/Verify.EntityFramework/)
[![NuGet Status](https://img.shields.io/nuget/v/Verify.EntityFrameworkClassic.svg)](https://www.nuget.org/packages/Verify.EntityFrameworkClassic/)

Extends [Verify](https://github.com/VerifyTests/Verify) to allow verification of EntityFramework bits.

Support is available via a [Tidelift Subscription](https://tidelift.com/subscription/pkg/nuget-verify?utm_source=nuget-verify&utm_medium=referral&utm_campaign=enterprise).

<a href='https://dotnetfoundation.org' alt='Part of the .NET Foundation'><img src='https://raw.githubusercontent.com/VerifyTests/Verify/master/docs/dotNetFoundation.svg' height='30px'></a><br>
Part of the <a href='https://dotnetfoundation.org' alt=''>.NET Foundation</a>

toc


## NuGet package

 * https://nuget.org/packages/Verify.EntityFramework/
 * https://nuget.org/packages/Verify.EntityFrameworkClassic/


## Usage

Enable VerifyEntityFramewok once at assembly load time:


### EF Core

snippet: EnableCore


### EF Classic

snippet: EnableClassic


### Recording

Recording allows all commands executed by EF to be captured and then (optionally verified).


#### Enable

Call `VerifyEntityFramework.EnableRecording()` on `DbContextOptionsBuilder`.


#### Usage

On the `DbContext` call

 * `VerifyEntityFramework.StartRecording()` to start recording.
 * `VerifyEntityFramework.FinishRecording()` to finish recording and get the results.

snippet: Recording

Will result in the following verified file:

snippet: CoreTests.Recording.verified.txt


### ChangeTracking

Added, deleted, and Modified entities can be verified by performing changes on a DbContext and then verifying the instance of ChangeTracking. This approach leverages the [EntityFramework ChangeTracker](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.changetracking.changetracker).


#### Added entity

This test:

snippet: Added

Will result in the following verified file:

snippet: CoreTests.Added.verified.txt


#### Deleted entity

This test:

snippet: Deleted

Will result in the following verified file:

snippet: CoreTests.Deleted.verified.txt


#### Modified entity

This test:

snippet: Modified

Will result in the following verified file:

snippet: CoreTests.Modified.verified.txt


### Queryable

This test:

snippet: Queryable

Will result in the following verified file:


### EF Core

snippet: CoreTests.Queryable.verified.txt


### EF Classic

snippet: ClassicTests.Queryable.verified.txt


## Security contact information

To report a security vulnerability, use the [Tidelift security contact](https://tidelift.com/security). Tidelift will coordinate the fix and disclosure.


## Icon

[Database](https://thenounproject.com/term/database/310841/) designed by [Creative Stall](https://thenounproject.com/creativestall/) from [The Noun Project](https://thenounproject.com/creativepriyanka).