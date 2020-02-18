# <img src="/src/icon.png" height="30px"> Verify.EntityFramework

[![Build status](https://ci.appveyor.com/api/projects/status/g6njwv0aox62atu0?svg=true)](https://ci.appveyor.com/project/SimonCropp/verify-entityframework)
[![NuGet Status](https://img.shields.io/nuget/v/Verify.EntityFramework.svg)](https://www.nuget.org/packages/Verify.EntityFramework/)
[![NuGet Status](https://img.shields.io/nuget/v/Verify.EntityFrameworkClassic.svg)](https://www.nuget.org/packages/Verify.EntityFrameworkClassic/)

Extends [Verify](https://github.com/SimonCropp/Verify) to allow verification of EntityFramework bits.

Support is available via a [Tidelift Subscription](https://tidelift.com/subscription/pkg/nuget-verify.entityframework?utm_source=nuget-verify.entityframework&utm_medium=referral&utm_campaign=enterprise).

toc


## NuGet package

 * https://nuget.org/packages/Verify.EntityFramework/
 * https://nuget.org/packages/Verify.EntityFrameworkClassic/


## Usage

Enable VerifyEntityFramewok once at assembly load time:

snippet: Enable


### ChangeTracking

Added, deleted, and Modified entities can be verified by performing changes on a DbContext and then verifying that context. This approach leverages the [EntityFramework ChangeTracker](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.changetracking.changetracker).


#### Added entity

This test:

snippet: Added

Will result in the following verified file:

snippet: Tests.Added.verified.txt


#### Deleted entity

This test:

snippet: Deleted

Will result in the following verified file:

snippet: Tests.Deleted.verified.txt


#### Modified entity

This test:

snippet: Modified

Will result in the following verified file:

snippet: Tests.Modified.verified.txt


### Queryable

This test:

snippet: Queryable

Will result in the following verified file:

snippet: Tests.Queryable.verified.txt


## Security contact information

To report a security vulnerability, use the [Tidelift security contact](https://tidelift.com/security). Tidelift will coordinate the fix and disclosure.


## Icon

[Database](https://thenounproject.com/term/database/310841/) designed by [Creative Stall](https://thenounproject.com/creativestall/) from [The Noun Project](https://thenounproject.com/creativepriyanka).