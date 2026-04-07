# Phase 9 - Step 7 Prep (ECommerce.Core Project Deletion)

Date: 2026-04-07
Branch: feature/phase-9-step-7-delete-core
Status: Ready for preflight only (high-risk deletion)

## Goal
Delete the legacy `ECommerce.Core` project after all remaining constants, enums, extensions, and contracts are migrated to bounded-context projects or `ECommerce.SharedKernel`.

This step is deletion-focused and high risk. Keep changes mechanical and behavior-preserving.

## Current Baseline (confirmed)
`ECommerce.Core` still exists and is still referenced by multiple projects:
- `src/backend/ECommerce.sln` includes `ECommerce.Core`
- `src/backend/ECommerce.API/ECommerce.API.csproj`
- `src/backend/ECommerce.Contracts/ECommerce.Contracts.csproj`
- `src/backend/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj`
- `src/backend/ECommerce.SharedKernel/ECommerce.SharedKernel.csproj`
- `src/backend/ECommerce.Tests/ECommerce.Tests.csproj`
- `src/backend/Catalog/ECommerce.Catalog.Application/ECommerce.Catalog.Application.csproj`
- `src/backend/Payments/ECommerce.Payments.Application/ECommerce.Payments.Application.csproj`
- `src/backend/Payments/ECommerce.Payments.Infrastructure/ECommerce.Payments.Infrastructure.csproj`

Step 6 completion state confirmed:
- `src/backend/ECommerce.Application/` is gone.
- No active project references to `ECommerce.Application.csproj` remain.

## Important Design Notes
1. Preserve behavior while changing ownership only:
   - no endpoint route changes
   - no wire-contract changes
   - no business-rule behavior changes
   - no schema/migration changes
2. Keep Clean Architecture flow intact.
3. Move dependencies first, delete only after compile-safety is proven.
4. Do not mix Step 7 with unrelated refactors.
5. Keep commits tiny and reversible.

## Step 7 Implementation Plan (tiny commits)
1. Migrate remaining Core constants/enums/extensions to approved owners
   - likely target: `ECommerce.SharedKernel` and/or BC-local contracts
   - update namespaces/usings only as needed
   - build gate

2. Remove `ECommerce.Core` project references from all `*.csproj`
   - update project references incrementally
   - build gate

3. Remove solution entry and delete project
   - remove `ECommerce.Core` from `src/backend/ECommerce.sln`
   - delete `src/backend/ECommerce.Core/`
   - build gate

4. Final verification and focused tests
   - run full backend build
   - run focused test filter for API/controller/auth/order/promo paths
   - confirm no `using ECommerce.Core` or `ECommerce.Core.csproj` references remain

## Verification Gates
Run after each mini-step:

Backend build:
- `dotnet build src/backend/ECommerce.sln`

Reference scans (use `rg`, or PowerShell fallback):
- `rg "using ECommerce.Core" src/backend --glob "*.cs"`
- `rg "ECommerce.Core.csproj" src/backend --glob "*.csproj"`
- `rg "ECommerce.Core\\ECommerce.Core.csproj" src/backend/ECommerce.sln`

Fallback scans if `rg` is unavailable:
- `Select-String -Path "src/backend/**/*.cs" -Pattern "using ECommerce.Core"`
- `Select-String -Path "src/backend/**/*.csproj" -Pattern "ECommerce.Core.csproj"`
- `Select-String -Path "src/backend/ECommerce.sln" -Pattern "ECommerce.Core\\ECommerce.Core.csproj"`

At step end:
- `dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "Controller|BackendGuideConventionsTests|Auth|Profile|PromoCodes|Orders"`

Expected at end of Step 7:
- `src/backend/ECommerce.Core/` deleted.
- No project references to `ECommerce.Core.csproj` remain.
- No production `using ECommerce.Core...` imports remain.
- Build and focused tests green.

## Not In Scope
- Any new feature work.
- API behavior redesign.
- Frontend changes.
- Phase 10 or other migrations.

## Execution Prompt (Ready To Implement)
Use this prompt as-is when starting Step 7 implementation.

You are a senior refactoring agent implementing Phase 9 Step 7 in this repository.

Mission:
- Remove `ECommerce.Core` as a project only after all active dependencies are migrated.
- Preserve runtime behavior and API contract behavior exactly.
- Complete only Step 7 scope.

Success criteria (all required):
1. `src/backend/ECommerce.Core/` is deleted.
2. `ECommerce.Core` project entry is removed from `src/backend/ECommerce.sln`.
3. All `*.csproj` references to `ECommerce.Core.csproj` are removed.
4. No production `using ECommerce.Core...` imports remain.
5. Build and focused tests are green.

Hard constraints:
- No endpoint route changes.
- No DTO wire contract changes.
- No authorization/authentication behavior changes.
- No schema/migration changes.
- Ignore unrelated frontend changes.

Execution rules:
- Use minimal, mechanical edits.
- Migrate ownership first; delete only after compile proves safety.
- Run required gate commands after each phase.
- If `rg` is unavailable, use PowerShell `Select-String` equivalents.

Preflight (must run first):
1. `dotnet build src/backend/ECommerce.sln`
2. `rg "using ECommerce.Core" src/backend --glob "*.cs"`
3. `rg "ECommerce.Core.csproj" src/backend --glob "*.csproj"`
4. `rg "ECommerce.Core\\ECommerce.Core.csproj" src/backend/ECommerce.sln`

Fallback preflight scans if `rg` is unavailable:
1. `Select-String -Path "src/backend/**/*.cs" -Pattern "using ECommerce.Core"`
2. `Select-String -Path "src/backend/**/*.csproj" -Pattern "ECommerce.Core.csproj"`
3. `Select-String -Path "src/backend/ECommerce.sln" -Pattern "ECommerce.Core\\ECommerce.Core.csproj"`

Preflight interpretation:
- If preflight build is red, stop and report first failing error.
- If dependency migration requires non-mechanical redesign, stop and report the minimal decision needed.

Execution phases (strict order):

Phase A - Migrate remaining Core ownership
1. Move remaining constants/enums/extensions/contracts from `ECommerce.Core` to approved owners.
2. Update namespaces/usings with minimal edits.
3. Gate: `dotnet build src/backend/ECommerce.sln`.

Phase B - Remove project references
1. Remove every `ECommerce.Core.csproj` reference from all `*.csproj` files.
2. Gate: `dotnet build src/backend/ECommerce.sln`.

Phase C - Delete project and solution entry
1. Delete `src/backend/ECommerce.Core/`.
2. Remove `ECommerce.Core` project entry from `src/backend/ECommerce.sln`.
3. Gate: `dotnet build src/backend/ECommerce.sln`.

Mandatory final verification:
1. `dotnet build src/backend/ECommerce.sln`
2. `dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "Controller|BackendGuideConventionsTests|Auth|Profile|PromoCodes|Orders"`
3. `rg "using ECommerce.Core" src/backend --glob "*.cs"`
4. `rg "ECommerce.Core.csproj" src/backend --glob "*.csproj"`
5. `rg "ECommerce.Core\\ECommerce.Core.csproj" src/backend/ECommerce.sln`

Fallback final scans if `rg` is unavailable:
1. `Select-String -Path "src/backend/**/*.cs" -Pattern "using ECommerce.Core"`
2. `Select-String -Path "src/backend/**/*.csproj" -Pattern "ECommerce.Core.csproj"`
3. `Select-String -Path "src/backend/ECommerce.sln" -Pattern "ECommerce.Core\\ECommerce.Core.csproj"`

Stop conditions (must stop and report):
- Non-mechanical redesign is required across multiple bounded contexts.
- Behavior-preserving migration is not possible without explicit product decision.
- Build/tests remain red after targeted Step 7 fixes.

Commit slicing (tiny logical commits):
1. `refactor(step7): migrate remaining core contracts and constants`
2. `refactor(step7): remove core project references`
3. `refactor(step7): delete ecommerce.core project`
4. `docs(phase-9): add step 7 prep execution prompt`

Failure report format:
1. Exact failed command
2. First failing error/test
3. Minimal patch proposal to recover

Final delivery format:
1. Files moved/deleted and minimal updates
2. Behavior-preservation confirmation
3. Build/test/scan summary
4. Commit hashes and messages
5. Residual risks and Phase 9 completion note
