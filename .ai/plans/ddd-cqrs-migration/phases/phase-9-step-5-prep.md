# Phase 9 - Step 5 Prep (Old Repository and Interface Deletion)

Date: 2026-04-07
Branch: feature/phase-9-step-5-delete-repositories
Status: Ready to implement (verification-heavy)

## Goal
Delete legacy monolith repositories and old core repository interfaces once all callers are confirmed migrated to bounded-context repositories.

This step is deletion-focused and high risk. Keep changes minimal and mechanical.

## Current Baseline (confirmed)
Legacy repository implementations still exist under:
- src/backend/ECommerce.Infrastructure/Repositories/CartRepository.cs
- src/backend/ECommerce.Infrastructure/Repositories/CategoryRepository.cs
- src/backend/ECommerce.Infrastructure/Repositories/OrderRepository.cs
- src/backend/ECommerce.Infrastructure/Repositories/ProductRepository.cs
- src/backend/ECommerce.Infrastructure/Repositories/Repository.cs
- src/backend/ECommerce.Infrastructure/Repositories/ReviewRepository.cs
- src/backend/ECommerce.Infrastructure/Repositories/UserRepository.cs
- src/backend/ECommerce.Infrastructure/Repositories/WishlistRepository.cs

Legacy core repository interfaces still exist under:
- src/backend/ECommerce.Core/Interfaces/Repositories/IRepository.cs
- src/backend/ECommerce.Core/Interfaces/Repositories/ICartRepository.cs
- src/backend/ECommerce.Core/Interfaces/Repositories/ICategoryRepository.cs
- src/backend/ECommerce.Core/Interfaces/Repositories/IOrderRepository.cs
- src/backend/ECommerce.Core/Interfaces/Repositories/IProductRepository.cs
- src/backend/ECommerce.Core/Interfaces/Repositories/IReviewRepository.cs
- src/backend/ECommerce.Core/Interfaces/Repositories/IUnitOfWork.cs
- src/backend/ECommerce.Core/Interfaces/Repositories/IUserRepository.cs
- src/backend/ECommerce.Core/Interfaces/Repositories/IWishlistRepository.cs

Known blocker still present:
- IInventoryService compatibility registration remains in API DI:
  - src/backend/ECommerce.API/Shared/Extensions/ServiceCollectionExtensions.cs
- Legacy inventory service contract/implementation still present:
  - src/backend/ECommerce.Application/Interfaces/IInventoryService.cs
  - src/backend/ECommerce.Application/Services/InventoryService.cs

Known DI coupling still present and must be handled in this step:
- src/backend/ECommerce.Infrastructure/UnitOfWork.cs
- src/backend/ECommerce.API/Shared/Extensions/ServiceCollectionExtensions.cs

## Important Design Notes
1. Deletion-focused only:
   - remove obsolete files and references
   - update DI/usings required for successful compile
   - no business logic changes
2. Preserve Clean Architecture direction:
   - API -> Application -> Core
   - Infrastructure -> Core/Application
3. Use BC repository interfaces only after deletion:
   - Ordering/Catalog/Identity/Shopping/Reviews repository interfaces from their BC domains
4. Do not start Step 6 or Step 7 in this branch.
5. Keep commit scope tight and reversible.

## Step 5 Implementation Plan (tiny commits)
1. Resolve inventory compatibility gap
   - Find current IInventoryService consumers
   - Remove compatibility registration once no runtime dependency remains
   - Build gate

2. Remove old infrastructure repository implementations
   - Delete legacy files in ECommerce.Infrastructure/Repositories
   - Remove any remaining references/usings
   - Build gate

3. Remove old core repository interfaces
   - Delete ECommerce.Core/Interfaces/Repositories files listed above
   - Replace any remaining usages with BC interfaces
   - Build gate

4. Remove obsolete UnitOfWork wiring and registrations
   - Clean ECommerce.Infrastructure/UnitOfWork usage if still tied to old interfaces
   - Remove MediatRUnitOfWork/old repository DI registration from ServiceCollectionExtensions if no longer needed
   - Build + focused tests gate

## Verification Gates
Run after each mini-step:

Backend build:
- dotnet build src/backend/ECommerce.sln

Step-specific scans:
- rg "ECommerce.Core.Interfaces.Repositories" src/backend --glob "*.cs"
- rg "ICartRepository|ICategoryRepository|IOrderRepository|IProductRepository|IReviewRepository|IUserRepository|IWishlistRepository" src/backend --glob "*.cs"
- rg "IInventoryService" src/backend --glob "*.cs"

At step end:
- dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "Controller|BackendGuideConventionsTests|UnitOfWork|InventoryService"

Expected at end of Step 5:
- Legacy repository files deleted from ECommerce.Infrastructure/Repositories.
- Legacy interfaces deleted from ECommerce.Core/Interfaces/Repositories.
- No production usage of ECommerce.Core.Interfaces.Repositories remains.
- No unresolved IInventoryService compatibility dependency remains.
- Build and focused tests green.

## Not In Scope
- Deleting ECommerce.Application (Step 6).
- Deleting ECommerce.Core (Step 7).
- Controller/folder reorganization (already handled in Steps 3-4).
- Feature behavior changes.

## Execution Prompt (Ready To Implement)
Use this prompt as-is when starting Step 5 implementation.

You are a senior refactoring agent implementing Phase 9 Step 5 in this repository.

Objective:
- Remove legacy monolith repository contracts and implementations.
- Remove obsolete dependency-injection wiring that depends on those contracts.
- Preserve runtime behavior and stay strictly within Step 5 scope.

Primary success criteria:
1. Deleted from `src/backend/ECommerce.Infrastructure/Repositories/`:
   - `CartRepository.cs`
   - `CategoryRepository.cs`
   - `OrderRepository.cs`
   - `ProductRepository.cs`
   - `Repository.cs`
   - `ReviewRepository.cs`
   - `UserRepository.cs`
   - `WishlistRepository.cs`
2. Deleted from `src/backend/ECommerce.Core/Interfaces/Repositories/`:
   - `IRepository.cs`
   - `ICartRepository.cs`
   - `ICategoryRepository.cs`
   - `IOrderRepository.cs`
   - `IProductRepository.cs`
   - `IReviewRepository.cs`
   - `IUnitOfWork.cs`
   - `IUserRepository.cs`
   - `IWishlistRepository.cs`
3. API/Infrastructure compiles cleanly after removal.
4. `IInventoryService` compatibility dependency is resolved (no unresolved runtime dependency remains).
5. Focused verification commands are green.

Non-goals (must not do):
- Do not start Step 6 (`ECommerce.Application` deletion).
- Do not start Step 7 (`ECommerce.Core` deletion outside listed Step 5 files).
- Do not change endpoint routes, DTO contracts, authorization rules, or business behavior.
- Do not perform opportunistic refactors/style cleanups unrelated to Step 5.
- Do not touch unrelated frontend changes.

Implementation constraints:
- Prefer minimal, mechanical edits.
- Preserve Clean Architecture direction and existing BC boundaries.
- If replacing a legacy interface usage, use the corresponding bounded-context interface.
- Do not use broad search-replace across the repository.

Preflight checklist (run and interpret before edits):
1. `rg "ECommerce.Core.Interfaces.Repositories" src/backend --glob "*.cs"`
2. `rg "IInventoryService" src/backend --glob "*.cs"`
3. `dotnet build src/backend/ECommerce.sln`

Preflight interpretation rules:
- If build is already failing, stop and report the first failure before making Step 5 edits.
- If `IInventoryService` has active production call sites, resolve that dependency first.

Execution phases (must follow in order):

Phase A - Resolve inventory compatibility dependency
1. Identify real consumers of `IInventoryService` (production code first, tests second).
2. Remove compatibility registration only after dependency is removed/migrated safely.
3. Run gate: `dotnet build src/backend/ECommerce.sln`.

Phase B - Delete old infrastructure repositories
1. Delete legacy files under `src/backend/ECommerce.Infrastructure/Repositories/`.
2. Apply only compile-fix fallout changes (usings, DI wiring, obsolete references).
3. Run gate: `dotnet build src/backend/ECommerce.sln`.

Phase C - Delete old core repository interfaces
1. Delete files under `src/backend/ECommerce.Core/Interfaces/Repositories/`.
2. Replace any remaining references with BC contracts where required.
3. Run gate: `dotnet build src/backend/ECommerce.sln`.

Phase D - Remove obsolete UnitOfWork and DI wiring
1. Clean `ECommerce.Infrastructure/UnitOfWork.cs` and related registrations if they depend on deleted contracts.
2. Remove obsolete MediatRUnitOfWork/legacy repository registrations from `ServiceCollectionExtensions` if unused.
3. Run gate: `dotnet build src/backend/ECommerce.sln`.

Mandatory verification (end of step):
1. `dotnet build src/backend/ECommerce.sln`
2. `dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "Controller|BackendGuideConventionsTests|UnitOfWork|InventoryService"`
3. `rg "ECommerce.Core.Interfaces.Repositories" src/backend --glob "*.cs"`
4. `rg "IInventoryService" src/backend --glob "*.cs"`

Expected verification state:
- Build is green.
- Focused tests are green.
- No production references to `ECommerce.Core.Interfaces.Repositories`.
- No unresolved production dependency on `IInventoryService`.

Stop conditions (must stop and report):
- More than one bounded context requires non-mechanical redesign.
- Any runtime behavior change is required to proceed.
- Build/test remains red after targeted Step 5 fixes.

Commit slicing (tiny logical commits):
1. `refactor(step5): remove inventory compatibility bridge dependency`
2. `refactor(step5): delete legacy infrastructure repositories`
3. `refactor(step5): delete legacy core repository interfaces`
4. `refactor(step5): remove obsolete unitofwork and di registrations`
5. `docs(phase-9): harden step 5 prep execution prompt`

Failure reporting format:
1. Exact failed command
2. First failing error/test
3. Minimal patch proposal to recover

Final delivery format:
1. Files deleted and files minimally updated
2. Confirmation of no intentional behavior changes
3. Build, test, and grep outputs summary
4. Commit list with hashes and messages
5. Residual risks and explicit Step 6 readiness note
