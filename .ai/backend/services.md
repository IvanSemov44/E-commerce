# Backend Services Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Keep service layer focused on business logic with consistent contracts and dependencies.

## Core Rules
1. Services inject `IUnitOfWork`, not individual repositories.
2. Services return `Result<T>` for expected business outcomes.
3. Use `ErrorCodes` constants for failure semantics.
4. Services coordinate transactions through UnitOfWork.
5. Services map entities to DTOs through AutoMapper.

## Dependency Pattern
Typical constructor dependencies:
- `IUnitOfWork`
- `IMapper`
- `ILogger<TService>`
- Additional domain services as required

## Real Code References
- Product service: `src/backend/ECommerce.Application/Services/ProductService.cs`
- Order service: `src/backend/ECommerce.Application/Services/OrderService.cs`
- Wishlist service: `src/backend/ECommerce.Application/Services/WishlistService.cs`
- Result model: `src/backend/ECommerce.Core/Results/Result.cs`
- Error codes: `src/backend/ECommerce.Core/Constants/ErrorCodes.cs`

## Behavior Rules
- Expected failure (not found, duplicate, invalid state): return `Result<T>.Fail(...)`.
- Success path: return `Result<T>.Ok(...)`.
- Unexpected infrastructure failure: let middleware/exception path handle.

## Common Mistakes
- Throwing exceptions for expected business outcomes.
- Returning entities directly rather than DTO contracts.
- Skipping cancellation token propagation.
- Embedding HTTP concerns in service layer.

## Checklist
- [ ] Service method returns `Result<T>` where appropriate.
- [ ] Failure path uses `ErrorCodes`.
- [ ] UnitOfWork handles commit.
- [ ] DTO mapping is centralized and consistent.
