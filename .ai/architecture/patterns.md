# Architecture Patterns

Updated: 2026-03-08
Owner: @ivans

## Purpose
Document the key cross-cutting patterns used in this codebase.

## Repository + Unit of Work
- Repositories encapsulate data access and query logic.
- `IUnitOfWork` provides repository access and commit boundaries.

References:
- `src/backend/ECommerce.Infrastructure/Repositories/Repository.cs`
- `src/backend/ECommerce.Infrastructure/UnitOfWork.cs`

## Result Pattern
- Services return `Result<T>` for expected business outcomes.
- Error code constants identify business failures.

References:
- `src/backend/ECommerce.Core/Results/Result.cs`
- `src/backend/ECommerce.Core/Constants/ErrorCodes.cs`

## Validation Pattern
- DTO validation via FluentValidation.
- API write endpoints guarded by validation filter.

References:
- `src/backend/ECommerce.Application/Validators/`
- `src/backend/ECommerce.API/ActionFilters/ValidationFilterAttribute.cs`

## Frontend Data Pattern
- Server state is RTK Query cache.
- UI-only state belongs in slices/local state.

References:
- `src/frontend/storefront/src/shared/lib/api/baseApi.ts`
- `src/frontend/storefront/src/features/`
