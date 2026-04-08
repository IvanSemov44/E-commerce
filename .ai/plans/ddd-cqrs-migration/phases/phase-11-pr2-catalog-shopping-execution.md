# Phase 11 PR 2 Execution: Catalog + Shopping DB Split Pilot

Status: In progress
Owner: @ivans
Created: 2026-04-08

## Scope

Target PR slice:
- PR 2: Catalog + Shopping DB split pilot

Contexts/entities/tables in scope:
- Catalog: Categories, Products, ProductImages
- Shopping: Carts, CartItems, Wishlists, product/inventory read projections used by shopping

Explicit out-of-scope:
- Identity, Ordering, Inventory, Promotions, Reviews, Payments runtime cutovers
- AppDbContext final business mapping removals (PR 5)

## Implemented in this slice (initial routing)

1. Catalog DI now prefers `ConnectionStrings:CatalogConnection` and falls back to `DefaultConnection`.
2. Shopping DI now prefers `ConnectionStrings:ShoppingConnection` and falls back to `DefaultConnection`.
3. API appsettings now includes `CatalogConnection` and `ShoppingConnection` keys.
4. Development appsettings points Catalog and Shopping to dedicated local databases.

## Files changed

- `src/backend/Catalog/ECommerce.Catalog.Infrastructure/DependencyInjection.cs`
- `src/backend/Shopping/ECommerce.Shopping.Infrastructure/DependencyInjection.cs`
- `src/backend/ECommerce.API/appsettings.json`
- `src/backend/ECommerce.API/appsettings.Development.json`

Docker-first bootstrap (no repo SQL files):

```powershell
# Start PostgreSQL container
docker compose up -d postgres

# Create DBs only if missing (safe for clean DB or already-existing DB)
docker compose exec postgres psql -U ecommerce -d postgres -c "SELECT 'CREATE DATABASE \"ECommerceCatalogDb\"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'ECommerceCatalogDb')\gexec"
docker compose exec postgres psql -U ecommerce -d postgres -c "SELECT 'CREATE DATABASE \"ECommerceShoppingDb\"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'ECommerceShoppingDb')\gexec"
```

## Remaining required work in PR 2

1. Create/apply context-specific migration streams for Catalog and Shopping (if not already separated enough for dedicated DB bootstrap).
2. Add dual-read verification scripts for pilot entities/tables.
3. Capture row-count/checksum evidence from shared DB vs target DB.
4. Run targeted integration tests for Catalog and Shopping flows.

## Validation gates

```powershell
cd src/backend

dotnet build ECommerce.API/ECommerce.API.csproj

dotnet test ECommerce.Tests/ECommerce.Tests.csproj --filter "FullyQualifiedName~Catalog|FullyQualifiedName~Shopping"
```

Validation status (current):
1. `dotnet build ECommerce.API/ECommerce.API.csproj` passed.
2. Dedicated test-fix slice completed: integration reliability tests were updated to use `IntegrationPersistenceDbContext`.
3. `dotnet test ECommerce.Tests/ECommerce.Tests.csproj --filter "FullyQualifiedName~Cart|FullyQualifiedName~Shopping"` passed (`total: 47, failed: 0, succeeded: 46, skipped: 1`).

Data verification commands (template):

```powershell
# Shared source DB
docker compose exec postgres psql -U ecommerce -d ECommerceDb -c "SELECT COUNT(*) AS products_count FROM public.\"Products\";"
docker compose exec postgres psql -U ecommerce -d ECommerceDb -c "SELECT COUNT(*) AS carts_count FROM shopping.\"Carts\";"
docker compose exec postgres psql -U ecommerce -d ECommerceDb -c "SELECT COUNT(*) AS cart_items_count FROM shopping.\"CartItems\";"
docker compose exec postgres psql -U ecommerce -d ECommerceDb -c "SELECT COUNT(*) AS wishlists_count FROM shopping.\"Wishlists\";"

# Target catalog DB
docker compose exec postgres psql -U ecommerce -d ECommerceCatalogDb -c "SELECT COUNT(*) AS products_count FROM public.\"Products\";"

# Target shopping DB
docker compose exec postgres psql -U ecommerce -d ECommerceShoppingDb -c "SELECT COUNT(*) AS carts_count FROM shopping.\"Carts\";"
docker compose exec postgres psql -U ecommerce -d ECommerceShoppingDb -c "SELECT COUNT(*) AS cart_items_count FROM shopping.\"CartItems\";"
docker compose exec postgres psql -U ecommerce -d ECommerceShoppingDb -c "SELECT COUNT(*) AS wishlists_count FROM shopping.\"Wishlists\";"
```

## Rollback plan

Trigger conditions:
1. Catalog/Shopping error rate rises above agreed threshold.
2. Data verification mismatch for pilot tables.
3. Projection lag or dead-letter behavior breaches threshold post-cutover.

Rollback steps:
1. Set `CatalogConnection` and `ShoppingConnection` to the shared DB endpoint (or remove overrides so fallback uses `DefaultConnection`).
2. Redeploy API with reverted env config.
3. Re-run row-count/checksum verification.

Post-rollback verification:
1. Catalog and Shopping endpoints return expected results.
2. No new dead-letter growth caused by pilot cutover.
3. Build/tests remain green.

## Evidence artifacts to attach in PR

1. Build output.
2. Targeted test output.
3. Row-count/checksum comparison output.
4. Metrics snapshot for projection lag/dead-letter during pilot window.
