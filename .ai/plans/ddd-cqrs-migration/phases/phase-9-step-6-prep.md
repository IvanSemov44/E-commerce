# Phase 9 - Step 6 Prep (ECommerce.Application Project Deletion)

Date: 2026-04-07
Branch: feature/phase-9-step-6-delete-application
Status: Ready to implement (post-Step 5)

## Goal
Delete the monolith application project after all remaining contracts, DTOs, validators, and cross-cutting services are migrated to bounded-context projects or shared layers.

This step is deletion-focused and high risk. Keep changes mechanical and behavior-preserving.

## Current Baseline (confirmed)
ECommerce.Application is still live and still referenced:
- Solution entry exists in src/backend/ECommerce.sln
- API project reference exists in src/backend/ECommerce.API/ECommerce.API.csproj
- Test project reference exists in src/backend/ECommerce.Tests/ECommerce.Tests.csproj
- Additional references exist in:
  - src/backend/Payments/ECommerce.Payments.Application/ECommerce.Payments.Application.csproj
  - src/backend/Ordering/ECommerce.Ordering.Infrastructure/ECommerce.Ordering.Infrastructure.csproj

High-impact dependency surface still using ECommerce.Application namespace includes:
- API controllers/middleware/action filters using ECommerce.Application.DTOs.*
- API and infrastructure using ECommerce.Application.Interfaces.*
- API service registration using MappingProfile and validators from ECommerce.Application
- Unit/integration tests importing ECommerce.Application DTOs/services/interfaces

Known migration items from phase plan that must be resolved before deletion:
- BusinessRulesOptions ownership
- CurrentUserService ownership
- DistributedIdempotencyStore ownership
- SendGridEmailService and SmtpEmailService ownership
- MappingProfile removal or replacement

## Important Design Notes
1. Preserve behavior, move ownership only:
   - No endpoint route changes
   - No business logic changes
   - No schema/migration changes
2. Keep clean architecture direction intact:
   - API -> Application -> Core
   - Infrastructure -> Core/Application
3. Minimize blast radius with phased migration:
   - migrate contracts first
   - delete project only when compile-safe
4. Do not start Step 7 in this branch.
5. Keep commit scope tiny and reversible.

## Step 6 Implementation Plan (tiny commits)
1. Move cross-cutting interfaces and implementations out of ECommerce.Application
   - ICurrentUserService + CurrentUserService
   - IIdempotencyStore + DistributedIdempotencyStore
   - IEmailService + SendGridEmailService + SmtpEmailService
   - BusinessRulesOptions
   - Update DI registrations and usings
   - Build gate

2. Migrate API-facing DTOs and validators still tied to ECommerce.Application
   - Move/replace DTO contracts to bounded contexts or shared contracts
   - Move/replace validators to target owners
   - Remove MappingProfile dependency if still used
   - Build gate

3. Remove project references to ECommerce.Application
   - Update all *.csproj files that reference ECommerce.Application.csproj
   - Update source imports/usings accordingly
   - Build gate

4. Delete ECommerce.Application project
   - Delete src/backend/ECommerce.Application directory
   - Remove ECommerce.Application from src/backend/ECommerce.sln
   - Build + focused tests gate

## Verification Gates
Run after each mini-step:

Backend build:
- dotnet build src/backend/ECommerce.sln

Reference scans (use rg, or PowerShell Select-String fallback if rg is unavailable):
- rg "using ECommerce.Application" src/backend --glob "*.cs"
- rg "ECommerce.Application.csproj" src/backend --glob "*.csproj"
- rg "ECommerce.Application\\ECommerce.Application.csproj" src/backend/ECommerce.sln
- Select-String -Path "src/backend/**/*.cs" -Pattern "using ECommerce.Application"
- Select-String -Path "src/backend/**/*.csproj" -Pattern "ECommerce.Application.csproj"
- Select-String -Path "src/backend/ECommerce.sln" -Pattern "ECommerce.Application\\ECommerce.Application.csproj"

At step end:
- dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "Controller|BackendGuideConventionsTests|Auth|Profile|PromoCodes"

Expected at end of Step 6:
- src/backend/ECommerce.Application directory removed.
- No project references to ECommerce.Application.csproj remain.
- No production source imports using ECommerce.Application remain.
- Build and focused tests green.

## Not In Scope
- Deleting ECommerce.Core (Step 7).
- Large API redesign or endpoint behavior changes.
- Frontend changes.

## Execution Prompt (Ready To Implement)
Use this prompt as-is when starting Step 6 implementation.

You are a senior refactoring agent implementing Phase 9 Step 6 in this repository.

Mission:
- Remove ECommerce.Application as a project only after all active dependencies are migrated.
- Preserve runtime behavior and API contract behavior exactly.
- Complete only Step 6 scope (no Step 7 work).

Success criteria (all required):
1. `src/backend/ECommerce.Application/` is deleted.
2. `ECommerce.Application` project entry is removed from `src/backend/ECommerce.sln`.
3. All `*.csproj` references to `ECommerce.Application.csproj` are removed.
4. No production `using ECommerce.Application...` imports remain.
5. Build and focused tests are green.

Hard constraints:
- No endpoint route changes.
- No DTO wire contract changes.
- No authorization/authentication behavior changes.
- No schema/migration changes.
- No Step 7 (`ECommerce.Core` deletion) work.
- Ignore unrelated frontend changes.

Execution rules:
- Use minimal, mechanical edits.
- Migrate ownership first, delete after compile proves safety.
- Run the required gate command after each phase before continuing.
- If `rg` is not installed, use `Select-String` equivalents.

Preflight (must run first):
1. `dotnet build src/backend/ECommerce.sln`
2. `rg "using ECommerce.Application" src/backend --glob "*.cs"`
3. `rg "ECommerce.Application.csproj" src/backend --glob "*.csproj"`
4. `rg "ECommerce.Application\\ECommerce.Application.csproj" src/backend/ECommerce.sln`

Fallback scans if `rg` is unavailable:
1. `Select-String -Path "src/backend/**/*.cs" -Pattern "using ECommerce.Application"`
2. `Select-String -Path "src/backend/**/*.csproj" -Pattern "ECommerce.Application.csproj"`
3. `Select-String -Path "src/backend/ECommerce.sln" -Pattern "ECommerce.Application\\ECommerce.Application.csproj"`

Preflight interpretation:
- If preflight build is red, stop and report first failing error.
- If a dependency move requires behavior change, stop and report minimal migration decision needed.

Execution phases (strict order):

Phase A - Migrate cross-cutting contracts/services
1. Migrate ownership of remaining cross-cutting interfaces/services/options currently in ECommerce.Application.
2. Update DI registrations and usings.
3. Gate: `dotnet build src/backend/ECommerce.sln`.

Phase B - Migrate DTO, validator, and mapping dependencies
1. Remove API/test dependencies on ECommerce.Application DTOs and validators by moving to shared or bounded-context owners.
2. Remove MappingProfile dependence if present.
3. Gate: `dotnet build src/backend/ECommerce.sln`.

Phase C - Remove project references
1. Remove every `ECommerce.Application.csproj` reference from all `*.csproj` files.
2. Gate: `dotnet build src/backend/ECommerce.sln`.

Phase D - Delete project and solution entry
1. Delete `src/backend/ECommerce.Application/`.
2. Remove the ECommerce.Application project entry from `src/backend/ECommerce.sln`.
3. Gate: `dotnet build src/backend/ECommerce.sln`.

Mandatory final verification:
1. `dotnet build src/backend/ECommerce.sln`
2. `dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "Controller|BackendGuideConventionsTests|Auth|Profile|PromoCodes"`
3. `rg "using ECommerce.Application" src/backend --glob "*.cs"`
4. `rg "ECommerce.Application.csproj" src/backend --glob "*.csproj"`
5. `rg "ECommerce.Application\\ECommerce.Application.csproj" src/backend/ECommerce.sln`

Fallback final scans if `rg` is unavailable:
1. `Select-String -Path "src/backend/**/*.cs" -Pattern "using ECommerce.Application"`
2. `Select-String -Path "src/backend/**/*.csproj" -Pattern "ECommerce.Application.csproj"`
3. `Select-String -Path "src/backend/ECommerce.sln" -Pattern "ECommerce.Application\\ECommerce.Application.csproj"`

Expected final state:
- Build is green.
- Focused tests are green.
- No source imports or project references to ECommerce.Application remain.

Stop conditions (must stop and report):
- Non-mechanical redesign is required across multiple bounded contexts.
- Behavior-preserving migration is not possible without explicit product decision.
- Build or focused tests remain red after targeted Step 6 fixes.

Commit slicing (tiny logical commits):
1. `refactor(step6): migrate cross-cutting application contracts`
2. `refactor(step6): migrate remaining application dto and validator usage`
3. `refactor(step6): remove application project references`
4. `refactor(step6): delete ecommerce.application project`
5. `docs(phase-9): harden step 6 prep execution prompt`

Failure report format:
1. Exact failed command
2. First failing error/test
3. Minimal patch proposal to recover

Final delivery format:
1. Files moved/deleted and minimal updates
2. Behavior-preservation confirmation
3. Build/test/scan summary
4. Commit hashes and messages
5. Residual risks and Step 7 readiness note
