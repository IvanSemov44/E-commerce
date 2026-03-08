# CLAUDE.md

Start here for this repository.

## Required Read Order
1. `.ai/README.md`
2. `.ai/workflows/adding-feature.md`
3. `.ai/workflows/database-migrations.md` (for schema/persistence changes)
4. `.ai/workflows/testing-strategy.md`
5. `.ai/workflows/deployment.md` (for environment/deploy changes)
6. `.ai/workflows/troubleshooting.md`
7. `.ai/backend/error-handling.md`
8. `.ai/reference/common-mistakes.md`

## Critical Rules (Do Not Violate)
- Follow Clean Architecture direction: API -> Application -> Core, Infrastructure -> Core/Application.
- Services inject `IUnitOfWork`, not individual repositories.
- Services return `Result<T>` for expected business outcomes.
- Repositories do not call `SaveChangesAsync`; UnitOfWork commits.
- Controllers stay thin and return `ApiResponse<T>`.
- Use `[ValidationFilter]` for DTO write endpoints.
- Use `ErrorCodes` constants for business failures.
- Backend async methods include `CancellationToken cancellationToken = default`.
- Frontend API calls use RTK Query (`baseApi.injectEndpoints`), not manual fetch in components.
- Frontend server state stays in RTK Query; slices manage UI state.

## Maintenance Rule
If you change an established pattern, update related `.ai/` docs in the same PR.
