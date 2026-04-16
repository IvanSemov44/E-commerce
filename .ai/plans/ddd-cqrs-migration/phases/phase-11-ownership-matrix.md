# Phase 11 Ownership Matrix

Status: Draft for PR 1
Owner: @ivans
Created: 2026-04-08

## Purpose

Single source of truth for write ownership and approved read patterns during per-context database split.

Rules:
1. Every business table has exactly one write owner context.
2. Cross-context reads use projection/query-api by default.
3. Any direct-read exception must be temporary and explicitly tracked.

## Columns

- `Object`: table/view/materialized view name
- `CurrentSchema`: schema used in current code mappings
- `Type`: table | view | materialized_view
- `WriteOwnerContext`: single write owner
- `TargetDatabase`: dedicated database name after split
- `ConsumerContexts`: allowed consumer contexts
- `AccessMode`: projection | query-api | direct-read (temporary only)
- `DirectCrossContextAccessAllowed`: no by default
- `Bridge`: none | temporary
- `RemovalPhase`: target removal phase for temporary exceptions
- `Notes`: constraints, caveats, validation requirements

## Matrix

| Object | CurrentSchema | Type | WriteOwnerContext | TargetDatabase | ConsumerContexts | AccessMode | DirectCrossContextAccessAllowed | Bridge | RemovalPhase | Notes |
|---|---|---|---|---|---|---|---|---|---|---|
| Categories | public | table | Catalog | CatalogDb | Catalog, Shopping, Ordering | projection | no | none | n/a | Catalog source of truth |
| Products | public | table | Catalog | CatalogDb | Shopping, Ordering, Promotions, Reviews, Dashboard | projection | no | none | n/a | Cross-context reads via local projections/read models |
| ProductImages | public | table | Catalog | CatalogDb | Shopping, Ordering, Dashboard | projection | no | none | n/a | Catalog-owned media metadata |
| Users | public | table | Identity | IdentityDb | Ordering, Reviews, Dashboard | projection | no | none | n/a | Identity-owned user/auth profile |
| RefreshTokens | public | table | Identity | IdentityDb | Identity | query-api | no | none | n/a | Security-sensitive; no cross-context direct read |
| Addresses | public | table | Identity | IdentityDb | Ordering | projection | no | none | n/a | Ordering uses local address read model |
| InventoryItems | inventory | table | Inventory | InventoryDb | Shopping, Ordering, Dashboard | projection | no | none | n/a | Inventory stock authority |
| InventoryItemLogs | inventory | table | Inventory | InventoryDb | Inventory, Operations | query-api | no | none | n/a | Inventory operational log entity |
| Carts | shopping | table | Shopping | ShoppingDb | Shopping | query-api | no | none | n/a | Shopping write model |
| CartItems | shopping | table | Shopping | ShoppingDb | Shopping | query-api | no | none | n/a | Shopping write model |
| Wishlists | shopping | table | Shopping | ShoppingDb | Shopping | query-api | no | none | n/a | Shopping write model |
| Orders | public | table | Ordering | OrderingDb | Payments, Inventory, Dashboard | projection | no | none | n/a | Ordering write authority |
| OrderItems | public | table | Ordering | OrderingDb | Payments, Inventory, Dashboard | projection | no | none | n/a | Ordering write authority |
| PromoCodes | promotions | table | Promotions | PromotionsDb | Ordering, Shopping, Dashboard | projection | no | none | n/a | Promotions write authority |
| Reviews | public | table | Reviews | ReviewsDb | Reviews, Dashboard | query-api | no | none | n/a | Reviews write model |
| ReviewProductProjections | public | table | Reviews | ReviewsDb | Reviews | direct-read | no | none | n/a | Permanent local projection fed by catalog events |
| Orders | public | table | Payments | PaymentsDb | Payments | query-api | no | none | n/a | Payments local order copy/read model |
| OrderItems | public | table | Payments | PaymentsDb | Payments | query-api | no | none | n/a | Payments local order item copy/read model |
| outbox_messages | integration | table | TechnicalContext | IntegrationDb | All (indirect) | projection | no | none | n/a | Integration reliability |
| inbox_messages | integration | table | TechnicalContext | IntegrationDb | TechnicalContext | direct-read | no | none | n/a | Idempotency tracking |
| dead_letter_messages | integration | table | TechnicalContext | IntegrationDb | TechnicalContext, Operations | direct-read | no | none | n/a | Replay and diagnostics |
| order_fulfillment_saga_states | integration | table | TechnicalContext | IntegrationDb | Ordering, Operations | projection | no | none | n/a | Process state only |

## PR 1 resolution status

1. Physical schema/table baseline captured from current DbContext mappings.
2. `ReviewProductProjections` decision: permanent local projection in Reviews context.
3. Remaining open decision: dashboard projection ownership model (technical context vs per-context read stores).

## Evidence references

- `src/backend/Catalog/ECommerce.Catalog.Infrastructure/Persistence/CatalogDbContext.cs`
- `src/backend/Identity/ECommerce.Identity.Infrastructure/Persistence/IdentityDbContext.cs`
- `src/backend/Inventory/ECommerce.Inventory.Infrastructure/Persistence/InventoryDbContext.cs`
- `src/backend/Shopping/ECommerce.Shopping.Infrastructure/Persistence/ShoppingDbContext.cs`
- `src/backend/Ordering/ECommerce.Ordering.Infrastructure/Persistence/OrderingDbContext.cs`
- `src/backend/Payments/ECommerce.Payments.Infrastructure/Persistence/PaymentsDbContext.cs`
- `src/backend/Promotions/ECommerce.Promotions.Infrastructure/Persistence/PromotionsDbContext.cs`
- `src/backend/Reviews/ECommerce.Reviews.Infrastructure/Persistence/Configurations/ProductReadModelConfiguration.cs`
- `src/backend/ECommerce.Infrastructure/Data/Configurations/EntityConfigurations.cs`
