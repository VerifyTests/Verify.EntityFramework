# Todo

## Bugs (EF Core)

- [ ] **`UseDescriptiveParameterNames` makes `SaveChanges` fail** (`DescriptiveParameterNameGenerator.cs`, `DescriptiveModificationCommand.cs`)
  - Duplicate names: columns `Phone` and `Phone2`, three rows in one batch. The `col + count` fallback (lines 48 and 72) is not checked against `allGenerated`, so `Phone2` is generated twice. Throws `ArgumentException: An item with the same key has already been added. Key: Phone2`.
  - Many-to-many: saving one post with two tags uses `ClrType.Name` of the join entity (`DescriptiveModificationCommand.cs:17`), producing `@Dictionary`2PostsId`. SQL Server: "Incorrect syntax near '`'".
  - Column names with spaces (`First Name`) are used verbatim, so the SQL is invalid.
  - Fix: keep only letters, digits and `_` from entity and column names; increase the counter until the name is unused.

- [ ] **`AllData()` duplicates rows and throws on many-to-many** (`VerifyEntityFramework.cs:9`)
  - Inheritance: derived rows are returned once per type in the hierarchy (Animal + Dog, 2 rows stored, 3 returned).
  - Implicit many-to-many throws "Cannot create a DbSet for 'Dictionary<string, object>' because it is configured as a shared-type entity type."
  - Fix: skip types with a base type, use `Set<T>(entityType.Name)` for shared-type entities, sort by `FindPrimaryKey()` instead of a reflected `Id` (tables without `Id` currently come back in database order).

- [ ] **`DisableRecording()` stays on across pooled leases** (`VerifyEntityFramework.cs:226`)
  - Stores `ContextId.InstanceId`, which survives returning to the pool. Next lease of the same instance recorded 0 entries.
  - Fix: store the whole `ContextId` (includes lease).

- [ ] **Queryables ending in `Include`/`ThenInclude`, or a bare `DbSet`, get no `.sql` file** (`Converters/QueryableConverter.cs:61`)
  - They are `IncludableQueryable<,>` / `InternalDbSet<>`, not `EntityQueryable<>`, so only results are written.
  - Fix: `target is IQueryable { Provider: IAsyncQueryProvider }`; use `queryable.ElementType` in `TryExecuteQueryable`.

- [ ] **`IgnoreNavigationProperties` misses many-to-many navigations** (`VerifyEntityFramework.cs:94`)
  - `Post.Tags` still serialized. Also iterate `GetSkipNavigations()`.
  - Question: owned navigations *are* ignored, so owned data (e.g. `Shop.Address`) disappears from snapshots. Probably exclude `ForeignKey.IsOwnership`.

- [ ] **After `Update()`, unchanged collections show as modified** (`Extensions.cs:50`)
  - `List<string>` showed `Original: [a, b], Current: [a, b]`; EF snapshots a copy and the check uses `Equals`.
  - Fix: `property.Metadata.GetValueComparer().Equals(original, current)`. Affects change-tracker output and InMemory SaveChanges recording.

## Bugs (Classic / EF6)

- [ ] **Queryable JSON converter never runs** (`Converters/QueryableConverter.cs:25`)
  - `CanConvert(Type)` passes the `Type` to `IsQueryable(object)` (`is IQueryable`), always false. Nested queries are executed and serialized as results, not SQL.

- [ ] **Scalar projections and non-EF queryables throw** (`QueryableSerializer.cs:2`)
  - `Select(_ => _.Id)` violates `where TEntity : class`.
  - File-converter predicate accepts any `IQueryable`, so `list.AsQueryable()` passed to `Verify` throws.
  - Fix: drop the constraint; only match `DbQuery<>`.

- [ ] **Parameter inlining corrupts SQL** (`QueryableSerializer.cs:8`)
  - Replacing `@p__linq__1` also hits `@p__linq__10` (11 params produced `'v1'0`).
  - Values use current culture (de-DE wrote `'1,5'`).
  - Fix: replace whole parameter names only; format with `InvariantCulture`; escape `'`.

- [ ] **Proxy type names in change-tracker output** (`Extensions.cs:26`)
  - With `virtual` navigations, entities loaded from the database show as `Company_E9EC9FE425D11A51E0…`.
  - Fix: `ObjectContext.GetObjectType(entry.Entity.GetType()).Name`.

## Performance

- [ ] **`LogEntry` built for every command even when not recording** (`LogCommandInterceptor.cs:77`)
  - Copies parameters and trims SQL, then `Recording.TryAdd` discards it. Check `IsRecording()` first, as `AddQuery`/`AddSaveChanges` do.

- [ ] **`ConcurrentBag` behind `DisableRecording` is slow and never shrinks** (`VerifyEntityFramework.cs:226`)
  - Every command snapshots the bag under a lock: 43 µs per command at 1,000 entries.
  - Fix: `ConcurrentDictionary<DbContextId, byte>` (also fixes the pooling bug).

- [ ] Optional: cache formatted SQL by text (~0.3 ms per statement warm; repeated statements are re-parsed).

## Minor

- [ ] `RecordingOptionsExtension.cs:22` uses `FirstOrDefault`, but DI resolves the last registration. A library that added its query compiler with `Add…` would be silently displaced. Use `LastOrDefault`.
- [ ] `VerifyEntityFramework.cs:88` error message: "wither" → "either", `Enable()` → `Initialize()`.
