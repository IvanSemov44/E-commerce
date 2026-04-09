# Quick Start for AI Assistants

Updated: 2026-03-08
Owner: @ivans

## Goal
Get enough project context in under 5 minutes to make safe, pattern-aligned code changes.

## Read in This Order
1. `.ai/README.md`
2. `.ai/workflows/adding-feature.md`
3. `.ai/workflows/database-migrations.md` (only for schema/persistence changes)
4. `.ai/workflows/testing-strategy.md`
5. `.ai/workflows/deployment.md` (for environment/deploy changes)
6. `.ai/workflows/troubleshooting.md`
7. `.ai/backend/error-handling.md`
8. `.ai/reference/common-mistakes.md`

## Module Read Order (Data + API Work)
1. `.ai/backend/repositories.md`
2. `.ai/backend/services.md`
3. `.ai/frontend/storefront/api-integration.md`
4. `.ai/frontend/storefront/type-safety.md`

## Hard Rules
- Keep Clean Architecture flow intact: API -> Application -> Core and Infrastructure -> Core/Application.
- Services use `IUnitOfWork` and return `Result<T>` for business outcomes.
- Repositories never call `SaveChangesAsync`; UnitOfWork commits.
- Controllers stay thin and return `ApiResponse<T>`.
- Use `ErrorCodes` constants for business failures.
- Use `ValidationFilterAttribute` for DTO write endpoints.
- Frontend API integration uses RTK Query (`baseApi.injectEndpoints`).
- Frontend server state belongs in RTK Query cache; slices are for UI state.

## Before You Code
- Confirm the target pattern from real references in `.ai/workflows/adding-feature.md`.
- Scan `.ai/reference/common-mistakes.md` and ensure none will be introduced.
- If changing persistence/schema, follow `.ai/workflows/database-migrations.md`.
- If behavior changes, choose test scope using `.ai/workflows/testing-strategy.md`.

## Definition of Done
- Code follows established patterns.
- Tests updated as needed.
- If a pattern changed, `.ai` docs are updated in the same PR.
