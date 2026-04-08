# Phase 10 PR 2 Slice Plan: BR-001 Reviews Product Existence Cutover

Status: Completed
Owner: @ivans
Created: 2026-04-08

## Execution result

- Implemented: Reviews `CatalogService` now uses MediatR query boundary (`GetProductByIdQuery`) instead of direct `AppDbContext` product table access.
- Build validation: `dotnet build src/backend/ECommerce.sln -v minimal` succeeded.
- Targeted test validation: 42 passed, 0 failed.

## Slice objective

Remove direct shared-context read from Reviews product existence check path.

Bridge targeted:
- BR-001 from temporary bridge register.

Current bridge:
- Reviews infrastructure `CatalogService` reads products via `AppDbContext`.

Target state for this slice:
- Reviews no longer queries Catalog business data through `AppDbContext`.
- Reviews uses a projection/read-boundary contract for product existence.

## Scope (single reversible slice)

In scope:
- Reviews product existence dependency chain.
- Supporting projection/read interface and adapter wiring needed for Reviews only.
- Tests for Reviews behavior and projection/replay expectations for this path.
- Artifact updates (matrix, allowlist, bridge register, PR notes).

Out of scope:
- Cross-context transaction coordinator removal (BR-002).
- Full AppDbContext elimination.
- Multi-context schema cutovers.

## Expected code touch points

Primary targets:
- `src/backend/Reviews/ECommerce.Reviews.Infrastructure/Services/CatalogService.cs`
- `src/backend/Reviews/ECommerce.Reviews.Infrastructure/DependencyInjection.cs`
- `src/backend/Reviews/ECommerce.Reviews.Application/Interfaces/ICatalogService.cs`
- `src/backend/Reviews/ECommerce.Reviews.Application/CommandHandlers/CreateReviewCommandHandler.cs`
- `src/backend/Reviews/ECommerce.Reviews.Application/QueryHandlers/GetProductReviewsQueryHandler.cs`
- `src/backend/Reviews/ECommerce.Reviews.Application/QueryHandlers/GetProductAverageRatingQueryHandler.cs`

Likely test updates:
- `src/backend/Reviews/ECommerce.Reviews.Tests/Application/ReviewsHandlerTests.cs`
- Add/adjust integration tests for Reviews product existence path.

## Implementation outline

1. Introduce/align a projection-oriented product existence reader contract for Reviews boundary checks.
2. Replace AppDbContext-backed direct product table check in Reviews infrastructure with projection/read-boundary access.
3. Keep handler behavior unchanged (still fail with ProductNotFound when product does not exist).
4. Update DI wiring in Reviews infrastructure.
5. Run tests and ensure no behavior regression.

## Validation plan

From repo root:

```powershell
git status --short
$matches = git ls-files | Select-String '^src/backend/src/'; if ($null -eq $matches) { '0' } else { $matches.Count }
```

From `src/backend`:

```powershell
dotnet build ECommerce.sln -v minimal
dotnet test ECommerce.sln
```

Targeted tests first (recommended):
- Reviews handler tests
- Reviews integration tests for product existence behavior

## Rollback plan

1. Revert PR 2 commit(s).
2. Restore prior Reviews `CatalogService` implementation and DI registration.
3. Re-run build and Reviews tests.

Rollback trigger conditions:
- Product existence checks regress for valid/invalid products.
- Projection/read boundary is unavailable in runtime and no safe fallback exists.

## Done criteria for this slice

1. BR-001 status moved to `removed` with test evidence.
2. No Reviews product existence read path uses `AppDbContext` business entity access.
3. Ownership matrix and allowlist updated to reflect cutover.
4. Bridge register notes include final removal evidence and PR reference.
