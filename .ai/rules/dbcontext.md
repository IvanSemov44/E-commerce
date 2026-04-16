# DbContext Rules

## Scope

Applies to: EF Core DbContext classes, entity type configurations, migrations, interceptors, model conventions, connection-string registration.

---

## Hard Rules

1. **One write owner per table.** No table is mapped as a writable entity in two different DbContexts. Cross-context reads use read models or event projections.

2. **DbContext maps only its own context entities.** No business entity from another bounded context appears as a `DbSet` or `OwnsOne` target in this context's DbContext.

3. **Entity configurations and DbContext types must align.** Every `IEntityTypeConfiguration<T>` targets the same CLR type used by the `DbSet<T>`. Mixed shared-kernel vs. context-domain types for the same table are forbidden.

4. **OnModelCreating uses one coherent strategy.** Either inline mapping or assembly-applied configurations — not both in the same context. No duplicated or contradictory mapping rules.

5. **Single-property value objects use `HasConversion`, not `OwnsOne`.** `Email`, `Slug`, `Sku`, `ProductName` map to one column via `HasConversion(to, from)`. `OwnsOne` on a single-property record creates shadow column names that silently break `HasIndex(...).IsUnique()`.

6. **Multi-property value objects use `OwnsOne` with explicit column names.** `Money`, `PersonName`, `DateRange` use `builder.OwnsOne(...)` with `.HasColumnName(...)` on every property. Never rely on EF auto-generated `PropertyName_FieldName` format.

7. **Enums are always stored as strings.** Every enum property has `.HasConversion<string>()`. Storing enums as integers risks silent data corruption on enum reorder.

8. **Aggregate private parameterless constructors must be empty.** EF calls the private `()` constructor during materialization. No validation, no events, no defaults in that constructor.

9. **Collections are backed by private fields.** `private readonly List<T> _field` backing field, exposed as `IReadOnlyCollection<T>`. EF accesses the backing field during materialization.

10. **Global query filters are applied exactly once per context.** Soft-delete, tenancy, and other filters must be present where expected and must not execute twice (e.g. not in both the context and a query layer).

11. **Interceptors and conventions are explicitly scoped to this context.** Audit field interceptors, concurrency hooks, and domain event dispatch hooks must be registered for this context and must not execute twice for the same operation.

---

## Allowed Patterns

- Design-time factory (`IDesignTimeDbContextFactory<T>`) for EF tooling. Must not be used as a runtime replacement.
- Context-specific connection key in DI, failing fast at startup if missing.
- Separate read-only DbContext or `AsNoTracking` query paths for query handlers.

---

## Forbidden Patterns

- `context.OtherContextEntity.Add(...)` — direct cross-context write.
- `OwnsOne` on a single-property `record` value object.
- `Property<int>("Status")` shadow property for an enum — stores as int, ignores the actual enum type.
- Fallback to a shared or unrelated connection key when the context-specific key is missing.
- Sharing an EF interceptor between contexts via a static or ambient registration.

---

## Required Tests

- Integration test confirming global query filters fire correctly (soft-delete filter hides deleted records, tenancy filter isolates rows).
- Integration test confirming audit/concurrency interceptor executes exactly once per operation.
- Migration smoke test: `dotnet ef migrations add` succeeds for this context in isolation.

---

## Required Evidence (before merge)

- `dotnet ef migrations add` output shows only this context's entities.
- Migration file, designer, and snapshot are present in this context's infrastructure project.
- Real database verified: tables, indexes, constraints, and migration history match expectations.
- No model drift (`dotnet ef migrations has-pending-model-changes` is clean).

---

## Definition of Done

All hard rules pass. Required tests are green. Required evidence is attached in the PR.
