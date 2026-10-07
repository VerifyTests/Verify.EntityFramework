# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Verify.EntityFramework extends the [Verify](https://github.com/VerifyTests/Verify) snapshot testing framework to support Entity Framework testing. It provides utilities for snapshot testing EF operations including change tracking, query execution, and SQL recording.

The repository contains two main packages:
- **Verify.EntityFramework** - For EF Core (modern)
- **Verify.EntityFrameworkClassic** - For EF 6 (legacy)

## Build Commands

```bash
# Build the solution
dotnet build src

# Run tests (excludes integration tests by default)
dotnet test src

# Run a specific test
dotnet test src --filter "FullyQualifiedName~CoreTests.Added"

# Run tests in a specific project
dotnet test src/Verify.EntityFramework.Tests
```

## Project Structure

```
src/
├── Verify.EntityFramework/           # EF Core library
│   ├── VerifyEntityFramework.cs      # Main public API
│   ├── Converters/                   # JSON converters for EF objects
│   │   ├── QueryableConverter.cs     # Converts IQueryable to SQL + results
│   │   ├── TrackerConverter.cs       # Converts ChangeTracker state
│   │   └── LogEntryConverter.cs      # Recorded SQL entries
│   └── LogCommandInterceptor.cs      # DbCommandInterceptor for SQL recording
│
├── Verify.EntityFramework.Tests/     # EF Core tests
├── Verify.EntityFrameworkClassic/    # EF 6 library
└── Verify.EntityFrameworkClassic.Tests/
```

## Architecture

### Module Initialization Pattern
Tests use `[ModuleInitializer]` to call `VerifyEntityFramework.Initialize(model)` once at assembly load, passing an `IModel` from a DbContext. This caches navigation property information for later use.

### Key Extension Points

1. **Converters** - Custom JSON converters inherit from `WriteOnlyJsonConverter<T>` and are registered globally via `VerifierSettings.RegisterFileConverter()` or added to `DefaultContractResolver.Converters`.

2. **Recording System** - `LogCommandInterceptor` implements `DbCommandInterceptor` to capture SQL operations. Enable via `builder.EnableRecording()` on `DbContextOptionsBuilder`. The InMemory provider executes no `DbCommand`, so `EnableRecording()` also adds `RecordingOptionsExtension`, which swaps in `RecordingQueryCompiler` (records queries as LINQ expressions) and `RecordingStateManager` (records SaveChanges). Both only record for InMemory, and only replace EF's default implementations.

3. **Queryable Verification** - When verifying an `IQueryable`, generates both a `.txt` file (query results) and a `.sql` file (the SQL query).

### Main API Methods (VerifyEntityFramework.cs)

- `Initialize(IModel)` - Required setup, caches model metadata
- `AllData()` - DbContext extension to enumerate all database entities
- `IgnoreNavigationProperties()` - Exclude EF navigation properties from serialization
- `EnableRecording()` - Enable SQL command recording on DbContextOptionsBuilder. Does not apply the anti-pattern checks, which are opt in, unless `throwOnAntiPatterns: true` or `VerifyEntityFramework.ThrowOnAntiPatternsByDefault = true`, which apply `ThrowOnAntiPatterns()`
- `DisableRecording()` - Stop recording for a specific context instance
- `UseDescriptiveTableAliases()` - Replace single-char SQL table aliases with full table names via custom `ISqlAliasManagerFactory`
- `ScrubInlineEfDateTimes()` - Sanitize DateTime values in SQL
- `ThrowOnAntiPatterns()` - Opt in anti-pattern checks. Every check has a `ThrowOnX` flag on `AntiPatternOptions`, off by default. With no arguments the standard set is switched on (`AntiPatternOptions.EnableStandard()`: the query detectors below plus `ThrowOnEfWarnings`); with a configure action only the checks it selects run, on top of those from an earlier call. The flags checked at query compile time are part of the service provider identity (`AntiPatternOptions.CompileFlags`). Adds `AntiPatternOptionsExtension`, which registers `AntiPatternInterceptor` (an `IQueryExpressionInterceptor`) as an `IInterceptor` service (not via `AddInterceptors`, which EF rejects with `UseInternalServiceProvider`). It throws when a query is compiled: `IgnoredEntityOperatorDetector` (Include/tracking options on a query that returns no entity) and `DiscardedOrderByDetector` (OrderBy followed by OrderBy), among others. `UnorderedFirstDetector` (First on a collection navigation in a subquery, without an ordering or filter) covers a gap in EF's `FirstWithoutOrderByAndFilterWarning` (dotnet/efcore#39129); the `EfIgnoresUnorderedFirstInSubquery` test fails once EF fixes it, meaning the detector can be removed. `ThrowOnEfWarnings` configures EF's own query warnings (`AntiPatternInterceptor.Warnings`) to throw. Runtime checks (`AntiPatternOptions`, passed to `ThrowOnAntiPatterns(configure)`) run in `RuntimeAntiPatternInterceptor` (commands, SaveChanges), with per context counts in the scoped `AntiPatternState`. By default they only count while Verify is recording (`AntiPatternOptions.OnlyWhileRecording`), so test setup and assertions are excluded

## Testing Conventions

- Uses NUnit with `[Parallelizable(ParallelScope.All)]`
- Snapshot files follow pattern: `TestClass.TestMethod.verified.txt`
- Async tests use `await Verify(...)` pattern
- Tests create in-memory SQLite databases or use SQL Server LocalDB

## Dependencies

- .NET 10.0 SDK (preview features enabled)
- C# language version: preview
- Key packages: Verify, Microsoft.EntityFrameworkCore, NUnit
