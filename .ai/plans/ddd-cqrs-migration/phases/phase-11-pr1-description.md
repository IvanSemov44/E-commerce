# Phase 11 PR 1: Decision and Ownership Freeze

## Summary

This PR establishes the non-code governance baseline required before per-context database cutovers begin.

Implemented artifacts:
1. `phase-11-ownership-matrix.md`
2. `phase-11-connection-string-contract.md`
3. `phase-11-appdbcontext-decision.md`
4. `prompts/phase-11/*` prompt pack for deterministic AI-assisted execution

## Scope

- In scope:
  - Ownership matrix with single write owner per table
  - Connection-string contract and transition map
  - AppDbContext decision and acceptance/reject criteria
  - Prompt and validation workflow for Phase 11 PR slices
- Out of scope:
  - Runtime code migration/cutover
  - Schema migration execution
  - New product features

## Key decisions

1. `AppDbContext` is not a business write-model context in end state.
2. Technical persistence is explicit (`IntegrationPersistenceDbContext`, `DataProtectionKeysContext`).
3. Connection contract transitions from `DefaultConnection` baseline to per-context keys by PR slice.
4. `ReviewProductProjections` is treated as a permanent local projection in Reviews.

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
- `src/backend/ECommerce.API/Shared/Extensions/ServiceCollectionExtensions.cs`
- `src/backend/ECommerce.API/appsettings.json`
- `src/backend/ECommerce.API/appsettings.Development.json`

## Risks and open item

1. Dashboard projection ownership model remains open (technical context vs per-context read stores).

## Rollback

Docs-only PR: revert commit.

## Checklist

- [x] Ownership matrix added/updated
- [x] Connection contract added/updated
- [x] AppDbContext decision documented
- [x] Prompt pack created for implementation/validation/rollback/evidence
- [ ] Architecture reviewer sign-off
- [ ] Dashboard projection ownership decision recorded
