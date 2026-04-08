# Phase 10 Temporary Bridge Register

Status: Draft for PR 1
Owner: @ivans
Created: 2026-04-08

## Purpose

Track temporary architecture bridges used during Phase 10.

Rule:
- No undocumented bridge is allowed.
- Every bridge must have a removal phase and verification test gate.

## Columns

- `BridgeId`: unique id (BR-###)
- `Type`: data-read | transaction | migration | projection
- `Location`: file/class/service where bridge exists
- `Owner`: accountable person
- `Reason`: why bridge exists
- `RemovalPhase`: target phase/slice
- `TestGate`: test(s) required before removal
- `Status`: open | in-progress | removed
- `Notes`: extra context

## Register

| BridgeId | Type | Location | Owner | Reason | RemovalPhase | TestGate | Status | Notes |
|---|---|---|---|---|---|---|---|---|
| BR-001 | data-read | Reviews infrastructure product existence path (shared-context read) | @ivans | Legacy compatibility while projection boundary is introduced | 10-PR2 | Reviews integration + projection replay tests | removed | Removed AppDbContext direct read and removed Catalog.Application compile coupling from Reviews infrastructure. Validation: solution build + targeted Reviews tests (42 passed, 0 failed). |
| BR-004 | data-read | Reviews `CatalogService` product existence via Reviews local projection table | @ivans | Transitional direct-read bridge while event-fed local projection table is introduced | 10-PR4 | Reviews integration + projection sync tests | removed | Dedicated `ReviewProductProjections` table + `ProductProjectionUpdatedIntegrationEventHandler` implemented. Validation: solution build + Reviews projection sync tests + Reviews integration tests (42 passed, 0 failed). |
| BR-005 | migration | Reviews local projection table bootstrap/backfill for existing product catalog | @ivans | Existing products may not have projection rows until replay/backfill runs | 10-PR5 | Backfill verification + Reviews integration tests | removed | Added `ReviewsProductProjectionBackfillService` and startup execution hook (`ApplyMigrationsAndSeedAsync`) for idempotent insert/update backfill from Catalog -> Reviews projections. Validation: targeted tests `ReviewsProductProjectionBackfillCharacterizationTests` + `ReviewsProductProjectionSyncCharacterizationTests` (4 passed, 0 failed). |
| BR-002 | transaction | Cross-context MediatR UoW coordinator in API behavior path | @ivans | Legacy command orchestration across multiple DbContexts | 10-PR3 | Transaction failure semantics tests | removed | Removed explicit multi-DbContext Begin/Commit/Rollback choreography from `CrossContextMediatRUnitOfWork`; transaction methods are now no-op and cross-context consistency is handled by local saves + outbox/event flow. Validation: `dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "FullyQualifiedName~Ordering|FullyQualifiedName~Outbox|FullyQualifiedName~Integration"` (415 total, 406 passed, 9 skipped, 0 failed). |
| BR-003 | migration | Shared AppDbContext business access in remaining legacy path(s) | @ivans | Incremental cutover safety | 10-PR2/PR5 | Characterization + cutover verification checklist | in-progress | Incremental slices completed: removed dormant `ReviewsMediatRUnitOfWork` (unused Reviews infrastructure class coupling Reviews to `AppDbContext` + cross-context transaction methods), cut over Payments runtime order read path from shared `AppDbContext` to local `PaymentsDbContext` (`PaymentOrderRepository` + DI/test-host wiring), removed shared `AppDbContext` from `CrossContextMediatRUnitOfWork` compatibility save aggregation, moved startup migration/seed/backfill orchestration behind `AppDbContextInitializationService` so `ApplicationBuilderExtensions` no longer directly manipulates `AppDbContext`/`DatabaseSeeder`, switched Data Protection key persistence from `PersistKeysToDbContext<AppDbContext>()` to dedicated `DataProtectionKeysContext`, moved AppDbContext + seeder registration ownership from API extensions to `ECommerce.Infrastructure.InfrastructureCompositionExtensions`, and introduced `IntegrationPersistenceDbContext` for outbox/inbox/dead-letter/saga services (`EfIntegrationEventOutbox`, `InboxIdempotencyProcessor`, `DeadLetterReplayService`, `OrderFulfillmentSagaService`, `OutboxDispatcherHostedService`) with UoW/test-host save wiring updated accordingly. Validation: `dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "FullyQualifiedName~Payment"` (44 total, 41 passed, 3 skipped, 0 failed), `dotnet build src/backend/ECommerce.API/ECommerce.API.csproj` (succeeded), and targeted slice builds for touched projects. Remaining shared `AppDbContext` usage is primarily migration host context and legacy shared read model persistence not yet split into dedicated integration migration context. |

## Exit criteria

Phase 10 cannot be marked done if:
1. Any bridge is `open` without approved follow-up and due phase.
2. Any removed bridge lacks passing `TestGate` evidence in PR notes.
