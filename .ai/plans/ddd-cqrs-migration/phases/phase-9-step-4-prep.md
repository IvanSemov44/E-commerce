# Phase 9 - Step 4 Prep (Shared Folder Reorganization)

Date: 2026-04-07
Branch: feature/phase-9-step-4-shared-folder
Status: Ready to implement

## Goal
Move API cross-cutting files from top-level folders into Shared, then update namespaces/usings with no behavior changes:
- Configuration/* -> Shared/Configuration/*
- Extensions/* -> Shared/Extensions/*
- Helpers/PaginationRequestNormalizer.cs -> Shared/Helpers/PaginationRequestNormalizer.cs

This is a mechanical reorganization step only.

## Current Baseline (confirmed)
Source folders/files currently still in old locations:

Configuration:
- src/backend/ECommerce.API/Configuration/AppConfiguration.cs
- src/backend/ECommerce.API/Configuration/CorsPolicyNames.cs
- src/backend/ECommerce.API/Configuration/JwtOptions.cs
- src/backend/ECommerce.API/Configuration/MonitoringOptions.cs
- src/backend/ECommerce.API/Configuration/RateLimitingOptions.cs

Extensions:
- src/backend/ECommerce.API/Extensions/ApplicationBuilderExtensions.cs
- src/backend/ECommerce.API/Extensions/ConfigurationExtensions.cs
- src/backend/ECommerce.API/Extensions/DatabaseSchemaValidator.cs
- src/backend/ECommerce.API/Extensions/LoggingExtensions.cs
- src/backend/ECommerce.API/Extensions/ResilienceExtensions.cs
- src/backend/ECommerce.API/Extensions/ResultExtensions.cs
- src/backend/ECommerce.API/Extensions/ServiceCollectionExtensions.cs

Helpers:
- src/backend/ECommerce.API/Helpers/PaginationRequestNormalizer.cs

Known references to update:
- Program.cs imports ECommerce.API.Extensions
- HealthChecks/MemoryHealthCheck.cs imports ECommerce.API.Configuration
- Feature controllers import ECommerce.API.Extensions and/or ECommerce.API.Helpers

## Important Design Notes
1. Mechanical move only:
   - move file
   - update namespace declaration
   - update usings where required
2. Keep all runtime behavior unchanged:
   - do not alter options binding logic
   - do not alter middleware ordering
   - do not alter extension method behavior
3. Keep existing folder ownership untouched:
   - ActionFilters, Behaviors, Middleware, HealthChecks stay where they are
4. Delete old folders only after they are empty:
   - src/backend/ECommerce.API/Configuration/
   - src/backend/ECommerce.API/Extensions/
   - src/backend/ECommerce.API/Helpers/

## Step 4 Implementation Plan (tiny commits)
1. Move Configuration files
   - Move to Shared/Configuration
   - Update namespaces and dependent usings
   - Build gate

2. Move Extensions files
   - Move to Shared/Extensions
   - Update namespaces and dependent usings (including Program.cs)
   - Build gate

3. Move Pagination helper and cleanup
   - Move helper to Shared/Helpers
   - Update controller usings
   - Delete now-empty old folders
   - Build + focused tests gate

## Verification Gates
Run after each mini-step:

Backend build:
- dotnet build src/backend/ECommerce.sln

At step end:
- dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "Controller|BackendGuideConventionsTests"

Migration checks (workspace grep):
- rg "using ECommerce.API.Configuration|using ECommerce.API.Extensions|using ECommerce.API.Helpers" src/backend/ECommerce.API --glob "*.cs"

Expected at end of Step 4:
- No files remain in old Configuration/Extensions/Helpers folders.
- Old folders removed.
- All references use Shared namespaces.
- Build and focused tests green.

## Not In Scope
- Step 5 repository/core deletion work.
- Any controller logic refactor.
- Any Application/Core project deletion.

## Execution Prompt (Ready To Implement)
Use this prompt as-is when starting Step 4 implementation.

You are implementing Phase 9 Step 4 in this repository.

Mission:
- Move API cross-cutting files from top-level folders into `Shared`.
- Update namespaces/usings only.
- Preserve runtime behavior exactly.

Success criteria:
- Files moved to:
  - `src/backend/ECommerce.API/Shared/Configuration/*`
  - `src/backend/ECommerce.API/Shared/Extensions/*`
  - `src/backend/ECommerce.API/Shared/Helpers/PaginationRequestNormalizer.cs`
- Old folders removed once empty:
  - `src/backend/ECommerce.API/Configuration/`
  - `src/backend/ECommerce.API/Extensions/`
  - `src/backend/ECommerce.API/Helpers/`
- Build and focused tests are green.

Hard constraints:
- No behavior changes in options/middleware/extensions.
- No route/controller logic changes.
- No Step 5+ work.
- Ignore unrelated frontend file changes.
- Mechanical refactor only: move file + namespace + required using updates.

Execution order (must follow):
1. Move Configuration
   - Move all `Configuration/*` to `Shared/Configuration/*`
   - Update namespaces from `ECommerce.API.Configuration` to `ECommerce.API.Shared.Configuration`
   - Update dependent usings

2. Move Extensions
   - Move all `Extensions/*` to `Shared/Extensions/*`
   - Update namespaces from `ECommerce.API.Extensions` to `ECommerce.API.Shared.Extensions`
   - Update dependent usings (including `Program.cs`)

3. Move Helper and cleanup
   - Move `Helpers/PaginationRequestNormalizer.cs` to `Shared/Helpers/`
   - Update namespace from `ECommerce.API.Helpers` to `ECommerce.API.Shared.Helpers`
   - Update dependent usings
   - Delete old folders only if empty

Required gate after each execution-order batch:
1. `dotnet build src/backend/ECommerce.sln`
2. If build fails:
   - Stop progression to next batch
   - Apply minimal move-related fixes only
   - Re-run build until green

Required final verification:
1. `dotnet build src/backend/ECommerce.sln`
2. `dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "Controller|BackendGuideConventionsTests"`
3. `rg "using ECommerce.API.Configuration|using ECommerce.API.Extensions|using ECommerce.API.Helpers" src/backend/ECommerce.API --glob "*.cs"`

Expected final verification state:
- Build green.
- Focused tests green.
- No stale `using ECommerce.API.Configuration|Extensions|Helpers` remain.

Commit slicing rules (tiny commits):
1. `refactor(api): move configuration files into shared`
2. `refactor(api): move extension files into shared`
3. `refactor(api): move pagination helper and remove legacy api folders`
4. `docs(phase-9): harden step 4 prep into execution prompt`

Failure protocol:
- If any gate fails after targeted fixes, stop and report:
  - exact failing command
  - first failing error
  - minimal next patch proposal

Final response format:
1. Files moved (source -> destination)
2. Namespace/usings-only confirmation
3. Build/test/grep results
4. Commits created (hash + message)
5. Residual risks and Step 5 handoff notes
