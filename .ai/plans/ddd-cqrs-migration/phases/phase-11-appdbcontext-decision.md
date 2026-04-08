# Phase 11 AppDbContext Decision

Status: Approved candidate for PR 1 review
Owner: @ivans
Created: 2026-04-08

## Decision statement

`AppDbContext` is no longer a business write-model context.
During Phase 11 it is either:
1. reduced to platform-only scope, or
2. retired after technical persistence is fully moved to dedicated contexts.

Chosen direction for current migration:
- Keep technical persistence explicit via `IntegrationPersistenceDbContext` and `DataProtectionKeysContext`.
- Remove remaining business-entity mappings/usages from `AppDbContext` by PR 5.

## Why this decision

1. Preserves bounded-context ownership and clean architecture boundaries.
2. Eliminates hidden coupling and pseudo shared-transaction assumptions.
3. Keeps rollback manageable by separating technical persistence concerns.

## Allowed in AppDbContext (transitional only)

1. Transitional mappings explicitly tracked for removal in Phase 11 PR slices.
2. No new business entity mappings.

## Forbidden immediately

1. New business DbSet additions in `AppDbContext`.
2. New repository/service runtime reads against `AppDbContext` for bounded-context business decisions.
3. Any new cross-context write behavior routed through `AppDbContext`.

## Required enforcement checks

1. Search gate: no new usages of `AppDbContext` in bounded-context runtime repositories.
2. Build gate: API build passes after wiring changes.
3. Test gate: focused integration tests pass for affected contexts.
4. Documentation gate: ownership matrix and connection contract updated in same PR.

## PR 1 acceptance criteria

1. `phase-11-ownership-matrix.md` is approved with one write owner per table.
2. `phase-11-connection-string-contract.md` is approved with transition map.
3. No new `AppDbContext` business mappings are introduced.
4. Any remaining `AppDbContext` business mappings are explicitly scheduled in PR2-PR5 slices.

## PR 1 reject criteria

1. Any table has ambiguous or dual write ownership.
2. Any new runtime path introduces direct cross-context business writes.
3. Connection contract lacks per-environment key ownership.

## Evidence references for current baseline

- `src/backend/ECommerce.Infrastructure/Integration/IntegrationPersistenceDbContext.cs`
- `src/backend/ECommerce.API/Services/DataProtectionKeysContext.cs`
- `src/backend/ECommerce.Infrastructure/Data/AppDbContext.cs`

## Exit criteria (Phase 11 completion)

1. `AppDbContext` contains no bounded-context business entities.
2. All runtime business paths use context-owned DbContexts only.
3. Remaining `AppDbContext` presence is either platform-only or removed entirely.
