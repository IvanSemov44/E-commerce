# Phase 9 - Step 3 Prep (Controller Folder Reorganization)

Date: 2026-04-07
Branch: feature/phase-9-step-3-controller-moves
Status: Ready to implement

## Goal
Move remaining API controllers from the flat `Controllers/` folder into vertical slices under `Features/{Context}/Controllers/` with namespace and using updates only.

This step is mechanical. No controller behavior changes are in scope.

## Current Baseline (confirmed)
Already migrated to `Features/`:
- `src/backend/ECommerce.API/Features/Payments/Controllers/PaymentsController.cs`
- `src/backend/ECommerce.API/Features/Dashboard/Controllers/DashboardController.cs`

Still in flat `Controllers/`:
- `src/backend/ECommerce.API/Controllers/PromoCodesController.cs`
- `src/backend/ECommerce.API/Controllers/ReviewsController.cs`
- `src/backend/ECommerce.API/Controllers/InventoryController.cs`
- `src/backend/ECommerce.API/Controllers/CatalogProductsController.cs`
- `src/backend/ECommerce.API/Controllers/CatalogCategoriesController.cs`
- `src/backend/ECommerce.API/Controllers/AuthController.cs`
- `src/backend/ECommerce.API/Controllers/ProfileController.cs`
- `src/backend/ECommerce.API/Controllers/CartController.cs`
- `src/backend/ECommerce.API/Controllers/WishlistController.cs`
- `src/backend/ECommerce.API/Controllers/OrdersController.cs`
- `src/backend/ECommerce.API/Controllers/IntegrationDeadLettersController.cs`

## Important Design Notes
1. Keep this as a pure move/refactor step:
   - only file move, namespace update, required using fixes.
   - do not change routes, endpoint signatures, or logic.
2. Keep existing architecture constraints from promo-style controllers unchanged:
   - `[ValidationFilter]` usage, `CancellationToken` parameters, `Result<T>` mapping style.
3. `IntegrationDeadLettersController` is infrastructure tooling and moves under `Features/IntegrationOps/Controllers/`.
4. Architecture test path assumptions must be updated in Step 3 as files move:
   - `src/backend/ECommerce.Tests/Unit/Architecture/BackendGuideConventionsTests.cs`

## Step 3 Implementation Plan (tiny commits)
1. Move Promotions/Reviews/Inventory controllers
   - Create folders under `Features/Promotions`, `Features/Reviews`, `Features/Inventory`
   - Move files and update namespaces/usings

2. Move Catalog + Identity controllers
   - Catalog: `CatalogProductsController`, `CatalogCategoriesController`
   - Identity: `AuthController`, `ProfileController`

3. Move Shopping + Ordering controllers
   - Shopping: `CartController`, `WishlistController`
   - Ordering: `OrdersController`

4. Move IntegrationOps controller and clean up
   - Move `IntegrationDeadLettersController` to `Features/IntegrationOps/Controllers/`
   - Delete `src/backend/ECommerce.API/Controllers/` once empty
   - Update architecture tests that read controller files from old paths

## Verification Gates
Run after each mini-step:

Backend build:
- `dotnet build src/backend/ECommerce.sln`

Controller-focused tests:
- `dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "Controller"`

Architecture-focused tests:
- `dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "BackendGuideConventionsTests"`

Migration checks (workspace grep):
- `rg "src/backend/ECommerce.API/Controllers/.*Controller.cs" src/backend/ECommerce.Tests --glob "*.cs"`

Expected at end of Step 3:
- `src/backend/ECommerce.API/Controllers/` removed.
- All production controllers live under `src/backend/ECommerce.API/Features/*/Controllers/`.
- Build and controller/architecture tests green.

## Not In Scope
- Step 4 shared folder moves (`Configuration`, `Extensions`, `Helpers`).
- Any service/repository deletion work from Steps 5-7.
- Business logic or API behavior modifications.
