# Phase 11 Connection String Contract

Status: Draft for PR 1
Owner: @ivans
Created: 2026-04-08

## Purpose

Define environment-agnostic configuration keys for per-context database ownership.

## Contract rules

1. One primary write connection per bounded context.
2. No context may default to another context's write connection.
3. Shared technical persistence has dedicated connection keys.
4. Every connection key must be set in dev/stage/prod before cutover.

## Current baseline (from code)

1. Most bounded-context infrastructures currently resolve `ConnectionStrings:DefaultConnection`.
2. Data protection resolves `DefaultConnection` first, then `DataProtectionConnection` fallback.

Baseline evidence:
- `src/backend/Catalog/ECommerce.Catalog.Infrastructure/DependencyInjection.cs`
- `src/backend/Identity/ECommerce.Identity.Infrastructure/DependencyInjection.cs`
- `src/backend/Inventory/ECommerce.Inventory.Infrastructure/DependencyInjection.cs`
- `src/backend/Ordering/ECommerce.Ordering.Infrastructure/DependencyInjection.cs`
- `src/backend/Promotions/ECommerce.Promotions.Infrastructure/DependencyInjection.cs`
- `src/backend/Reviews/ECommerce.Reviews.Infrastructure/DependencyInjection.cs`
- `src/backend/Shopping/ECommerce.Shopping.Infrastructure/DependencyInjection.cs`
- `src/backend/ECommerce.API/Shared/Extensions/ServiceCollectionExtensions.cs`
- `src/backend/ECommerce.API/appsettings.json`
- `src/backend/ECommerce.API/appsettings.Development.json`

## Required keys

| Context | Config key | Required | Notes |
|---|---|---|---|
| Catalog | `ConnectionStrings:CatalogConnection` | yes | Catalog write model DB |
| Identity | `ConnectionStrings:IdentityConnection` | yes | Identity write model DB |
| Inventory | `ConnectionStrings:InventoryConnection` | yes | Inventory write model DB |
| Shopping | `ConnectionStrings:ShoppingConnection` | yes | Shopping write model DB |
| Ordering | `ConnectionStrings:OrderingConnection` | yes | Ordering write model DB |
| Promotions | `ConnectionStrings:PromotionsConnection` | yes | Promotions write model DB |
| Reviews | `ConnectionStrings:ReviewsConnection` | yes | Reviews write model DB |
| Payments | `ConnectionStrings:PaymentsConnection` | yes | Payments write model DB |
| Integration/Technical | `ConnectionStrings:IntegrationConnection` | yes | Outbox/inbox/dead-letter/saga state |
| DataProtection | `ConnectionStrings:DataProtectionConnection` | yes | ASP.NET Core Data Protection key ring |

## Transition map (PR order)

| PR slice | Current key usage | Target key usage | Required action |
|---|---|---|---|
| PR 1 | `DefaultConnection` | no runtime switch | Approve contract and key naming |
| PR 2 (Catalog + Shopping) | `DefaultConnection` | `CatalogConnection`, `ShoppingConnection` | Update DI registration and env secrets |
| PR 3 (Identity + Ordering) | `DefaultConnection` | `IdentityConnection`, `OrderingConnection` | Update DI registration and env secrets |
| PR 4 (Inventory + Promotions + Reviews + Payments) | `DefaultConnection` | `InventoryConnection`, `PromotionsConnection`, `ReviewsConnection`, `PaymentsConnection` | Update DI registration and env secrets |
| PR 5 (shared finalization) | `DefaultConnection` for shared technical remnants | `IntegrationConnection`, `DataProtectionConnection` | Remove business-routing dependency on shared default |

## Optional read-only/verification keys

| Purpose | Config key | Notes |
|---|---|---|
| Cutover dual-read source | `ConnectionStrings:LegacySharedConnection` | Temporary verification only; no new writes |
| Analytics/read replica | `ConnectionStrings:ReportingReadOnlyConnection` | Optional, non-authoritative |

## Environment checklist

For each environment (dev/stage/prod):
1. All required keys exist and resolve to reachable database endpoints.
2. Principle of least privilege applied per context credentials.
3. Secret storage configured (no plaintext secrets in repo).
4. Health checks validate each required connection.

## Validation commands (example)

```powershell
# Build config consumers
cd src/backend

dotnet build ECommerce.API/ECommerce.API.csproj

# Run focused integration set after wiring updates

dotnet test ECommerce.Tests/ECommerce.Tests.csproj --filter "FullyQualifiedName~Integration"
```

## Rollback configuration rule

If cutover fails, restore prior connection routing by environment override only.
No hot edits to code path are allowed during rollback unless incident commander approves.
