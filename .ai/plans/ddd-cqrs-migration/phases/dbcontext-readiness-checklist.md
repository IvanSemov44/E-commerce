# DbContext Readiness Checklist (Per Bounded Context)

Use this checklist to verify one bounded context is truly ready for independent persistence and migrations.

Companion checklist for cross-cutting reliability and boundary risks:
- `.ai/plans/ddd-cqrs-migration/checklists/cross-cutting-persistence-reliability-checklist.md`

Canonical checklist index:
- `.ai/plans/ddd-cqrs-migration/checklists/README.md`

## 1. Context ownership is explicit
- Define exactly which tables/entities this context owns for writes.
- Must match:
1. No table has two write owners.
2. Cross-context reads are event/read-model based, not direct write coupling.

## 2. DbContext contains only context-owned model
- Map only this context's business entities, plus explicitly allowed technical/read models.
- Must match:
1. No accidental shared AppDb business mappings.
2. No direct write mapping for other contexts' aggregates.

## 3. Entity configurations and DbContext types are consistent
- Configuration classes must target the same CLR entity types used by DbContext.
- Must match:
1. DbSet types and IEntityTypeConfiguration types are aligned.
2. No mixed shared-kernel versus context-domain type mapping for same tables.

## 4. OnModelCreating uses one coherent strategy
- Use either inline mapping only, or assembly-applied configurations, but keep it consistent.
- Must match:
1. No duplicated/contradictory mapping rules.
2. Table/schema names are deterministic.

## 5. Runtime DI registration is strict
- Register context with AddDbContext using required context-specific key.
- Must match:
1. Missing key fails fast.
2. No fallback to unrelated keys for context persistence.

## 6. Connection-string contract exists in configuration
- Add explicit connection key in config and environment overrides.
- Must match:
1. DI key name equals configuration key name.
2. Dev/stage/prod strategy documented.

## 7. Design-time migration creation path works
- Ensure dotnet ef tooling can create migrations for this context.
- Must match:
1. Startup project and context project resolve.
2. Design-time factory exists when needed by tooling.
3. Design-time factory is for EF tooling, not runtime replacement.

## 8. Context-specific migration stream exists
- Generate migrations from this context/project.
- Must match:
1. Migration file, designer, and snapshot are present.
2. Files live in this context infrastructure project.

## 9. Migration apply path is defined
- Define where migrations are applied (startup initializer or deployment pipeline).
- Must match:
1. Repeatable in all environments.
2. Failures halt safely and visibly.

## 10. Seeder ownership is context-local
- Seed only from context-owned path.
- Must match:
1. No hidden dependency on shared AppDb business seeders.
2. Seeding is idempotent.

## 11. Repository and UnitOfWork rules are preserved
- Service layer keeps repository and unit-of-work boundaries.
- Must match:
1. Repositories do not call SaveChangesAsync directly.
2. UnitOfWork owns commit boundary.

## 12. Cross-context effects use reliability patterns
- Use outbox/inbox/saga for cross-context consistency.
- Must match:
1. No direct cross-context business writes.
2. Idempotency and duplicate handling verified.
3. Dead-letter and replay path verified.

## 13. Test gate is green for touched context
- Run build and focused integration/reliability tests.
- Must match:
1. Build passed.
2. Context-focused tests passed.
3. Event/projection tests passed where applicable.

## 14. Database verification evidence exists
- Verify real database state, not just code.
- Must match:
1. Tables/indexes/constraints exist as expected.
2. Migration history is updated.
3. No unresolved model drift.

## 15. Rollback procedure is explicit
- Document rollback and recovery checks.
- Must match:
1. Trigger conditions defined.
2. Exact rollback steps listed.
3. Post-rollback integrity checks listed.

## 16. Operations evidence is captured
- Attach proof in PR/checklists.
- Must match:
1. Build/test logs linked.
2. Verification outputs linked.
3. Runbook/metrics references attached when applicable.

## Minimum Definition of Ready
A context is ready only when all 16 checks are satisfied with concrete evidence.





Low: coverage gap on new aggregate overloads.
New methods introduced in Category.cs:52 and Product.cs:95 are not directly unit-tested at domain level.
Current tests catch them indirectly through handlers, but direct domain tests would better protect invariants and error mapping.