# Todo

Status: **merged** (PR number), **pushed** (branch not yet merged), or open.

Anti-patterns that are not flagged yet. Each was checked against EF Core 10.0.12, on SQL Server LocalDB and InMemory, with `ThrowOnAntiPatterns()` on. Each new check needs tests (`AntiPatternTests` or `RuntimeAntiPatternTests`) and a readme section.


## Query checks (default on)

- [x] **`First`/`FirstOrDefault`/`SingleOrDefault` in a subquery, without `OrderBy` or a filter**: `First`/`FirstOrDefault` done in `UnorderedFirstDetector`, with the `EfIgnoresUnorderedFirstInSubquery` test failing once EF fixes it. `SingleOrDefault` left out, since its `TOP(1)` is by design, so it would outlive the EF fix.
  - EF logs `FirstWithoutOrderByAndFilterWarning` only when the query has no predicate and no ordering (`RelationalQueryableMethodTranslatingExpressionVisitor`). A collection navigation is expanded to a `Where` that correlates it with its parent, which counts as a predicate, so a subquery over a navigation never warns. `Take`/`Skip` only check the ordering, so they do warn. Arguably a false negative in EF; the team accepts the heuristic is imprecise (dotnet/efcore#29782). Reported as dotnet/efcore#39129.
  - `Select(c => new { First = c.Employees.FirstOrDefault()!.Name })` produces `SELECT TOP(1) [e].[Name] FROM [Employees] AS [e] WHERE [c].[Id] = [e].[CompanyId]`, with no `ORDER BY`, so the row returned is arbitrary. Snapshots of it are flaky.
  - Same in a `Where` (`c.Employees.FirstOrDefault()!.Age > 26`), and after a `Select` (`c.Employees.Select(e => e.Name).FirstOrDefault()`).
  - `SingleOrDefault()` in a subquery is also translated to `TOP(1)`, so it does not check that there is only one.
  - Detect: only inside lambdas, since EF covers the root. A call with no predicate, over a chain with no ordering and no `Where`, from a collection navigation or a `DbSet`.
  - Treat any `Where` as a filter, like EF does. That misses a correlating `Where`, like `data.Employees.Where(e => e.CompanyId == c.Id).Select(e => e.Name).FirstOrDefault()`, which is also unordered, but it can not be told apart from a filter that selects one row.
  - Not covered by EfOrderBy, which only orders the root chain and `Include` lambdas.
  - Already handled: `Take`/`Skip` in a subquery (EF's `RowLimitingOperationWithoutOrderByWarning` throws), and `GroupBy(...).Select(g => g.First())` (EF 10 orders the `ROW_NUMBER` window by the key).

- [x] **`OrderBy` before `Single`/`SingleOrDefault`**: done
  - `OrderBy(_ => _.Name).SingleOrDefault(_ => _.Id == 1)` produces `SELECT TOP(2) ... WHERE [c].[Id] = 1 ORDER BY [c].[Name]`. The sort runs, but the result of `Single` does not depend on order.
  - Add both to `DiscardedOrderByDetector.IsOrderIndependent`.

- [x] **`OrderBy` before `ExecuteDelete`/`ExecuteUpdate`**: done
  - EF drops the ordering. `Where(_ => _.Id < 0).OrderBy(_ => _.Name).ExecuteDeleteAsync()` produces `DELETE FROM [c] FROM [Companies] AS [c] WHERE [c].[Id] IN (SELECT [c0].[Id] FROM [Companies] AS [c0] WHERE [c0].[Id] < 0)`: no ordering, and a needless subquery. Without the `OrderBy` it is `WHERE [c].[Id] < 0`.
  - `ExecuteUpdate` produces an `INNER JOIN` to a subquery, also with no ordering.
  - Both are declared on `EntityFrameworkQueryableExtensions` in EF 10, so `DiscardedOrderByDetector.IsLinq` does not match them.
  - An ordering followed by `Take` is kept, since it chooses which rows are changed.

- [x] **Ordering by a constant**: done, in `ConstantOrderingDetector`
  - `OrderBy(_ => 1)`, or ordering by a captured variable, produces `ORDER BY (SELECT 1)`, so the rows are not ordered.
  - It also silences `RowLimitingOperationWithoutOrderByWarning`, which `ThrowOnAntiPatterns()` throws on, so it is the obvious workaround.
  - Detect: an `OrderBy`, `OrderByDescending`, `ThenBy`, or `ThenByDescending` whose key selector does not use its parameter.

- [ ] **`OrderBy` before `GroupBy`, then an aggregate**
  - `OrderBy(_ => _.Name).GroupBy(_ => _.CompanyId).Select(_ => new { _.Key, Count = _.Count() })` produces `SELECT [e].[CompanyId] AS [Key], COUNT(*) AS [Count] FROM [Employees] AS [e] GROUP BY [e].[CompanyId]`. The ordering is dropped.
  - Lower confidence. Check what EF does with a final `GroupBy`, one that returns the groups, before matching it. Safest to only match a `GroupBy` followed by a `Select`, or with a result selector.


## Fixes to existing checks

- [ ] **`IgnoredEntityOperatorDetector` misses a projection into an entity type**
  - `Include(_ => _.Employees).AsNoTracking().Select(c => new Company { Id = c.Id, Name = c.Name })` is not flagged, though EF ignores both: it produces `SELECT [c].[Id], [c].[Name] FROM [Companies] AS [c]`, and nothing is tracked.
  - `ReturnsEntities` returns early, since `Select` keeps the element type, and `EntityFinder` treats the `new Company { ... }` as an entity.
  - A `new` of an entity type is built by the projection, so is not an entity EF loads, tracks, or includes into. Its bindings can still hold real entities, for example `new Company { Employees = c.Employees }`.


## Model warnings (default on)

EF logs these, but does not throw. Add them to `AntiPatternInterceptor.Warnings`. A SQL Server one has to be built from its id and name, like `DecimalTypeDefaultWarning`. The model is built once per context type, so each test needs its own context type.

- [ ] **Relationship and change tracking mistakes**, each confirmed to fire:
  - `CoreEventId.CollectionWithoutComparer`: a collection with a value converter, but no value comparer. Changes to its elements are not detected, so are silently not saved.
  - `CoreEventId.ShadowForeignKeyPropertyCreated`: for example `Post.BlogId1` is created in shadow state, since `Post.BlogId` is a `string` and `Blog.Id` is an `int`.
  - `CoreEventId.ConflictingShadowForeignKeysWarning`: two relationships between `Person` and `Pet` without foreign key properties, so the shadow property names depend on discovery order.

- [ ] **Configuration that is silently ignored**:
  - `CoreEventId.AmbiguousEndRequiredWarning` (confirmed): `IsRequired()` before the dependent side is known.
  - `CoreEventId.RequiredAttributeOnCollection` (confirmed, logged at Debug level), and `CoreEventId.RequiredAttributeOnSkipNavigation`.
  - `CoreEventId.NoEntityTypeConfigurationsWarning` (confirmed): `ApplyConfigurationsFromAssembly` found no configuration, usually since it was passed the wrong assembly.
  - `CoreEventId.SkippedEntityTypeConfigurationWarning`: a configuration without a parameterless constructor is never applied.
  - `CoreEventId.MappedPropertyIgnoredWarning` (confirmed), `CoreEventId.MappedNavigationIgnoredWarning`, `CoreEventId.MappedEntityTypeIgnoredWarning`, and `CoreEventId.MappedComplexPropertyIgnoredWarning`: mapped, then ignored.

- [ ] **Relationships split, or not configured, by conflicting attributes**:
  - `CoreEventId.ConflictingForeignKeyAttributesOnNavigationAndPropertyWarning`
  - `CoreEventId.ForeignKeyAttributesOnBothNavigationsWarning`
  - `CoreEventId.ForeignKeyAttributesOnBothPropertiesWarning`
  - `CoreEventId.MultipleInversePropertiesSameTargetWarning`
  - `CoreEventId.NonOwnershipInverseNavigationWarning`

- [ ] **Other model and mapping mistakes**:
  - `CoreEventId.OldModelVersionWarning`: a compiled model built by an older version of EF.
  - `CoreEventId.RedundantForeignKeyWarning`: a foreign key that targets itself.
  - `CoreEventId.ConflictingKeylessAndKeyAttributesWarning`
  - `CoreEventId.AccidentalComplexPropertyCollection`
  - `RelationalEventId.AllIndexPropertiesNotToMappedToAnyTable`: the index is never created.
  - `RelationalEventId.KeyPropertiesNotMappedToTable`: logged at Error level, but not thrown.
  - `RelationalEventId.DuplicateColumnOrders`: logged at Error level, but not thrown.
  - `RelationalEventId.ForeignKeyTpcPrincipalWarning`
  - `RelationalEventId.TpcStoreGeneratedIdentityWarning`
  - `RelationalEventId.TriggerOnNonRootTphEntity`
  - `RelationalEventId.UnexpectedTrailingResultSetWhenSaving`: logged by `SaveChanges`, when a stored procedure returns a result set that is not configured.

- Leave out:
  - `SqlServerEventId.DecimalTypeKeyWarning`: logged for every decimal key, whatever its configuration, so a legacy `numeric(18,0)` key would throw.
  - `SqlServerEventId.SavepointsDisabledBecauseOfMARS`: depends on the test connection string.
  - `CoreEventId.SensitiveDataLoggingEnabledWarning` and `RelationalEventId.ColumnOrderIgnoredWarning`: normal in tests.
  - Migration and scaffolding events.

- Already thrown by EF 10 by default, so nothing to add: `CoreEventId.ManyServiceProvidersCreatedWarning`, `CoreEventId.AccidentalEntityType`, `CoreEventId.InvalidIncludePathError`, `CoreEventId.NavigationBaseIncludeIgnored`, `CoreEventId.LazyLoadOnDisposedContextWarning`, `CoreEventId.DetachedLazyLoadingWarning`, `RelationalEventId.IndexPropertiesBothMappedAndNotMappedToTable`, `RelationalEventId.IndexPropertiesMappedToNonOverlappingTables`, `RelationalEventId.ForeignKeyPropertiesMappedToUnrelatedTables`, `RelationalEventId.StoredProcedureConcurrencyTokenNotMapped`, `RelationalEventId.PendingModelChangesWarning`, `RelationalEventId.AmbientTransactionWarning`, `SqlServerEventId.ConflictingValueGenerationStrategiesWarning`, and `InMemoryEventId.TransactionIgnoredWarning`.


## Runtime checks (opt in)

- [ ] **`Update()`, or `State = Modified`, on an entity the context already tracks**
  - Load an `Employee`, change `Name`, then call `Update()`: all 6 non-key properties are marked modified, but only `Name` differs from its original value. The `UPDATE` writes every column, which can overwrite concurrent changes to the other columns, and makes triggers see every column as updated. Without `Update()`, only `Name` is marked.
  - Detect in `SavingChanges`, for a `Modified` entry: every non-key property that can be saved is marked modified, at least one differs from its original value, and at least one does not. That needs at least two such properties. Compare with the property's `ValueComparer`, like `Extensions.ChangedProperties`. Fits `RuntimeAntiPatternInterceptor.CheckSave`.
  - Not matched: a disconnected `Update(dto)`, or `AsNoTracking()` then `Update()`, since the original value equals the current value for every property. `Attach()` then marking one property. A value changed, then changed back, since only that property stays marked.
  - False positive: attaching as `Modified`, then changing a value before saving.
  - Missed: `Update()` on a tracked entity with no change, which looks like the disconnected case.
  - Both could be fixed by recording, in an `IMaterializationInterceptor`, the instances a tracking query loads, and only checking those.

- [ ] **`ExecuteUpdate`/`ExecuteDelete` in a loop**
  - `RuntimeAntiPatternInterceptor` does not override `NonQueryExecutingAsync`, and `CheckRepeated` only counts `CommandSource.LinqQuery` and `CommandSource.FromSqlQuery`. So `foreach (var id in ids) await data.Companies.Where(_ => _.Id == id).ExecuteDeleteAsync();` is not caught.
  - Also count `CommandSource.ExecuteDelete` and `CommandSource.ExecuteUpdate`, in `NonQueryExecuting` and `NonQueryExecutingAsync`. The message can suggest `Contains`, to change them all in one statement.
  - Either as part of `ThrowOnRepeatedQueries`, or with its own flag.


## Query checks (opt in)

- [ ] **Non unique ordering before `Skip`/`Take`**
  - `OrderBy(_ => _.Name).Skip(1).Take(1)` produces `ORDER BY [c].[Name] OFFSET @p ROWS FETCH NEXT @p ROWS ONLY`. When names tie, pages can repeat or skip rows.
  - EF 10 appends the key for a split query (`ORDER BY [c].[Name], [c].[Id]` in both queries), but not for a single query.
  - Detect: the ordering does not include every property of the primary key, or of an alternate key. Not a unique index, since a filter or a nullable column can make it non unique, the same reasoning as EfQueryComplexity's lookups by key. Only when the source returns each entity once, like `RedundantDistinctDetector.ReturnsEachEntityOnce`.
  - Might fit EfOrderBy better, by appending the key.
  - Related: a `ThenBy` after an ordering that already includes the key does nothing, for example `OrderBy(_ => _.Id).ThenBy(_ => _.Name)` produces `ORDER BY [c].[Id], [c].[Name]`.

- [ ] **Functions on a column in a filter**: generalize `ThrowOnColumnCaseConversion`
  - Estimated plans on LocalDB: `SUBSTRING([Code], 0 + 1, 2) = N'AB'` and `DATEPART(year, [Created]) = 2020` scan the index. `[Code] LIKE N'AB%'`, and `[Created] >= '2020-01-01' AND [Created] < '2021-01-01'`, seek it.
  - Suggest `_.Code.StartsWith(value)` for `_.Code.Substring(0, n) == value`, and a date range for `_.Created.Year == 2020`.
  - Not a problem: `_.Created.Date == date`, since SQL Server seeks through `CONVERT(date, ...)`.

- [ ] **`DateTime.Now`, `DateTime.UtcNow`, or `DateTime.Today` in a query**
  - `Where(_ => _.Created <= DateTime.Now)` produces `WHERE [e].[Created] <= GETDATE()`. That is the database server's clock and time zone, not the app's, and a fake `TimeProvider` can not control it in a test.
  - Suggest a variable, which EF sends as a parameter.
  - Opinionated, so opt in.

- [ ] **Projection into an entity type**
  - `Select(c => new Company { Id = c.Id, Name = c.Name })` produces `SELECT [c].[Id], [c].[Name]`, and nothing is tracked. The partial entities look like real ones, so passing one to `Update()` later writes defaults over the columns that were not selected. EF6 threw for this.
  - Opt in, since building a different entity type to insert, for example copying rows into an archive table, is fine. Maybe only match the source's own entity type.
  - See also the `IgnoredEntityOperatorDetector` fix above.


## Ignored or redundant operators (default on, rare)

- [ ] **`Include` of an owned navigation**
  - `Include(_ => _.Address)`, where `Address` is owned, produces the same SQL as without it, since owned types are always loaded with their owner.
  - An `AutoInclude` navigation is probably the same. Not checked.

- [ ] **Duplicate `Include`, or one that is a prefix of another**
  - `Include(_ => _.Employees).Include(_ => _.Employees)`, and `Include(_ => _.Company).Include(_ => _.Company.Employees)`: EF merges them silently.
  - Compare whole paths, an `Include` with its `ThenInclude`s, so the usual `Include(_ => _.A).ThenInclude(_ => _.B)` then `Include(_ => _.A).ThenInclude(_ => _.C)` is not flagged.

- [ ] **`IgnoreQueryFilters()` with no query filter**
  - No entity type in the query has a filter, including auto includes. With EF 10 named filters, also a name that no entity type in the query defines.

- [ ] **`AsNoTracking()` or `AsTracking()` on a keyless entity type**
  - Keyless entities are never tracked. `IgnoredEntityOperatorDetector.IsEntity` treats them as entities.

- [ ] **Null check on a required property**
  - `_.Name != null && _.Name.StartsWith("A")`, where `Name` is required, produces `WHERE [e].[Name] LIKE N'A%'`. EF drops the null check.
  - `RedundantNullCheckDetector` only matches a comparison with a constant, so misses this.
  - Only when the property is read from the lambda parameter itself, and the source returns entities of that type directly. After a left join (`DefaultIfEmpty`, `LeftJoin`) the entity can be null, and a property read through a cast to a derived type is null for other types.
  - Same for a required reference navigation, `_.Company != null`.


## Checked, not anti-patterns

- `_.Age == someLong` produces `CAST([e].[Age] AS bigint) = @age`, but SQL Server still seeks the index. A `short` column compared to an `int` is sent as `smallint`, with no cast on the column.
- `c.Employees.FirstOrDefault(pred) != null` is already translated to `EXISTS`.
- `Take` or `Skip` without `OrderBy` in a subquery, or in a filtered `Include`: EF's `RowLimitingOperationWithoutOrderByWarning` already throws.
- `GroupBy(...).Select(g => g.First())`: EF 10 orders the `ROW_NUMBER` window by the key.
- `AsSplitQuery()` with a non unique ordering and `Skip`/`Take`: EF 10 appends the key.


## Docs

- [x] **`NavigationBaseIncludeIgnored` is thrown by EF 10 by default**
  - The readme's "EF warnings" section said EF only logs it. A context without `ThrowOnAntiPatterns()` throws for it. Moved out of that list, with a note. Kept in `Warnings`, so it still throws when configured otherwise before `ThrowOnAntiPatterns()`.

- [x] **`DetachedLazyLoadingWarning` is thrown by EF 10 by default**
  - EF's default configuration sets it to throw. Fixed the comment on `AntiPatternInterceptor.LazyLoadingWarnings`, which said it would throw from inside Verify.
  - In EF 10 neither path raised it: an entity from a no tracking query lazy loads, and an entity detached with `State = Detached` loads nothing, silently. So the readme and `AntiPatternOptions.ThrowOnLazyLoading` are left as they are.
