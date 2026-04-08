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

## Execution Prompt (Ready To Implement)
Use this prompt as-is when starting Step 3 implementation.

You are implementing Phase 9 Step 3 in this repository.

Mission:
- Move all remaining API controllers from `src/backend/ECommerce.API/Controllers/` to `src/backend/ECommerce.API/Features/{Context}/Controllers/`.
- Apply namespace and required using updates only.
- Preserve behavior exactly.

Success criteria:
- Old `src/backend/ECommerce.API/Controllers/` folder is removed.
- All production controllers exist under `Features/*/Controllers/`.
- Build and required tests are green.

Hard constraints:
- No route changes.
- No endpoint signature changes.
- No auth/role logic changes.
- No DTO/schema changes.
- No business logic changes.
- No Step 4+ scope work.
- Do not edit frontend files.

Execution order (must follow):
1. Promotions, Reviews, Inventory
   - `PromoCodesController` -> `Features/Promotions/Controllers/`
   - `ReviewsController` -> `Features/Reviews/Controllers/`
   - `InventoryController` -> `Features/Inventory/Controllers/`

2. Catalog, Identity
   - `CatalogProductsController` + `CatalogCategoriesController` -> `Features/Catalog/Controllers/`
   - `AuthController` + `ProfileController` -> `Features/Identity/Controllers/`

3. Shopping, Ordering
   - `CartController` + `WishlistController` -> `Features/Shopping/Controllers/`
   - `OrdersController` -> `Features/Ordering/Controllers/`

4. IntegrationOps and cleanup
   - `IntegrationDeadLettersController` -> `Features/IntegrationOps/Controllers/`
   - Delete old `Controllers/` folder only after it is empty
   - Update architecture tests that assume old controller file paths

Required gate after each execution-order batch:
1. `dotnet build src/backend/ECommerce.sln`
2. If build fails:
   - Stop progression to next batch
   - Fix only move-related compile issues
   - Re-run build until green

Required final verification:
1. `dotnet build src/backend/ECommerce.sln`
2. `dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "Controller"`
3. `dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "BackendGuideConventionsTests"`
4. `rg "ECommerce.API/Controllers/.*Controller.cs" src/backend --glob "*.cs"`

Expected final verification state:
- Build green.
- Controller-focused tests green.
- Architecture conventions tests green.
- No production controllers remain in old `Controllers/` path.

Commit slicing rules (tiny commits):
1. `refactor(api): move promotions reviews inventory controllers to features`
2. `refactor(api): move catalog and identity controllers to features`
3. `refactor(api): move shopping and ordering controllers to features`
4. `refactor(api): move integrationops controller and remove legacy controllers folder`
5. `test(architecture): update controller path assumptions for feature folders`

Failure protocol:
- If any test/build gate fails after reasonable targeted fixes, stop and report:
  - exact failing command
  - first failing test or error
  - minimal next patch proposal

Final response format:
1. Files moved (source -> destination)
2. Namespace/usings-only confirmation
3. Build and test command results
4. Commits created (hash + message)
5. Residual risks or Step 4 handoff notes
