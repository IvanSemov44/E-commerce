# Copilot Instructions - E-Commerce

Read this first in every coding task:

1. `.ai/README.md`
2. `.ai/workflows/adding-feature.md`
3. `.ai/workflows/database-migrations.md` (for schema/persistence changes)
4. `.ai/workflows/testing-strategy.md`
5. `.ai/workflows/deployment.md` (for environment/deploy changes)
6. `.ai/workflows/troubleshooting.md`
7. `.ai/backend/error-handling.md`
8. `.ai/reference/common-mistakes.md`

## Critical Project Rules
- Keep Clean Architecture dependency flow intact.
- Service layer uses `IUnitOfWork` and returns `Result<T>` for business outcomes.
- Do not call `SaveChangesAsync` in repositories.
- Controllers are thin and return `ApiResponse<T>`.
- Use `ErrorCodes` constants for business failures.
- Use `ValidationFilterAttribute` on DTO write endpoints.
- Frontend API integration uses RTK Query (`baseApi.injectEndpoints`).
- Frontend server data belongs to RTK Query cache; slices are for UI state.

## Maintenance
If code changes an established pattern, update related `.ai/` docs in the same PR.
