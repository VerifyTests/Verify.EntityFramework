# Todo

Status: **merged** (PR number), **pushed** (branch not yet merged), or open.

## Bugs (EF Core)

- [x] **`UseDescriptiveParameterNames` makes `SaveChanges` fail**: merged #1070
  - Duplicate names from the unchecked counter fallback (`Phone`/`Phone2`, three rows), `@Dictionary`2PostsId` for implicit many-to-many join rows, and column names with spaces.
  - Fixed: names reduced to letters, digits and `_`; prefix from `ShortName()`; counter increases until unused.

- [x] **`AllData()` duplicates rows and throws on many-to-many**: merged #1071
  - Fixed: only root types queried, shared-type entities queried by name, rows ordered by primary key in the query.

- [x] **`DisableRecording()` stays on across pooled leases**: merged #1072
  - Fixed: stores the whole `ContextId` (includes lease).

- [x] **Queryables ending in `Include`/`ThenInclude`, or a bare `DbSet`, get no `.sql` file**: merged #1075
  - Fixed: `IIncludableQueryable<,>` and `DbSet<>` recognized; `ElementType` used in `TryExecuteQueryable`.

- [x] **`IgnoreNavigationProperties` misses many-to-many navigations**: merged #1073
  - Fixed: `GetSkipNavigations()` included.

- [x] **`IgnoreNavigationProperties` drops owned types**: pushed `keep-owned-types-in-ignore-navigations`
  - Ownership navigation from owner is kept; navigation from an owned type back to its owner is still ignored.
  - Behaviour change: owned data now appears in snapshots. Mention in release notes.

- [x] **After `Update()`, unchanged collections show as modified**: merged #1074
  - Fixed: compared with the property's `ValueComparer`.

## Bugs (Classic / EF6)

- [x] **Queryable JSON converter never runs**: merged #1076
  - Fixed: `CanConvert` matches `DbQuery<>`.

- [x] **Scalar projections and non-EF queryables throw**: merged #1077
  - Fixed: class constraint removed; file converter only matches `DbQuery<>`.

- [x] **Parameter inlining corrupts SQL**: merged #1078
  - Fixed: whole parameter names matched; invariant culture; `'` escaped.

- [x] **Null parameter inlined as `''`**: pushed `fix-classic-null-parameter`
  - Now inlined as `NULL`. Behaviour change: snapshots with a null parameter change. Mention in release notes.

- [x] **Proxy type names in change-tracker output**: pushed `fix-classic-proxy-names`
  - Fixed: name from `ObjectContext.GetObjectType`.

## Performance

- [x] **`LogEntry` built for every command even when not recording**: pushed `perf-check-is-recording-first`
  - Fixed: `IsRecording()` checked first.

- [x] **`ConcurrentBag` behind `DisableRecording` is slow and never shrinks**: merged #1072
  - Fixed: `ConcurrentDictionary<DbContextId, byte>`.

- [ ] Optional: cache formatted SQL by text (~0.3 ms per statement warm; repeated statements are re-parsed). Only worth doing if formatting shows up as slow.

## Minor

- [x] `RecordingOptionsExtension` checked the first registration instead of the last: pushed `fix-replace-default-last-registration`
- [x] Error message: "wither" → "either", `Enable()` → `Initialize()`: pushed `fix-navigations-error-message`

## Release notes

Breaking changes:

- `EnableRecording()` now throws on query anti-patterns (ignored `Include`/tracking options, discarded `OrderBy`, and EF's own query warnings such as `Take` without `OrderBy`). Opt out per context with `EnableRecording(throwOnAntiPatterns: false)`, or globally with `VerifyEntityFramework.ThrowOnAntiPatternsByDefault = false`. `ThrowOnAntiPatterns()` enables it for contexts that do not record.
- `EnableRecording()` and `EnableRecording(string? identifier)` merged into `EnableRecording(string? identifier = null, bool? throwOnAntiPatterns = null)`. Source compatible, not binary compatible.

Behaviour changes that alter existing snapshots:

- Owned types are kept by `IgnoreNavigationProperties`.
- Classic null parameters are inlined as `NULL` instead of `''`.
- A `DbSet` nested in a verified object is written as `{ Sql, Result }` instead of a list.
- `UseDescriptiveParameterNames`: the third occurrence of a column is now numbered from 1 (`Phone1`, previously `Phone2`).
