# Common Mistakes in This Codebase

Updated: 2026-03-08
Owner: @ivans

## Purpose
Capture high-frequency mistakes so AI and humans avoid repeating technical debt patterns.

## Mistakes and Corrections

1. Services injecting repositories directly
- Wrong: service constructor injects `IProductRepository` / `IOrderRepository` directly.
- Correct: inject `IUnitOfWork` and access repos through it.
- Reference: `src/backend/ECommerce.Application/Services/OrderService.cs`

2. Calling `SaveChangesAsync` inside repository methods
- Wrong: repository writes directly call save.
- Correct: repositories mutate/query only; `UnitOfWork.SaveChangesAsync` commits.
- Reference: `src/backend/ECommerce.Infrastructure/Repositories/Repository.cs`, `src/backend/ECommerce.Infrastructure/UnitOfWork.cs`

3. Missing `AsNoTracking()` for read-only queries
- Wrong: always tracking reads.
- Correct: apply non-tracking when `trackChanges` is false.
- Reference: `src/backend/ECommerce.Infrastructure/Repositories/ProductRepository.cs`

4. Relying on implicit/lazy loading
- Wrong: read aggregate without explicit includes.
- Correct: specialized repository adds `.Include()` chain for required graph.
- Reference: `src/backend/ECommerce.Infrastructure/Repositories/OrderRepository.cs`

5. Throwing for expected business failures
- Wrong: throw for not found/invalid business state in service.
- Correct: return `Result<T>.Fail(...)` with `ErrorCodes`.
- Reference: `src/backend/ECommerce.Application/Services/WishlistService.cs`

6. Using string status instead of enums
- Wrong: `string Status` fields.
- Correct: use enums like `OrderStatus`, `PaymentStatus`.
- Reference: `src/backend/ECommerce.Core/Entities/Order.cs`, `src/backend/ECommerce.Core/Enums/OrderStatus.cs`

7. Direct fetch calls in frontend feature components
- Wrong: components call `fetch`/manual API logic.
- Correct: use RTK Query endpoints via `baseApi.injectEndpoints`.
- Reference: `src/frontend/storefront/src/shared/lib/api/baseApi.ts`, `src/frontend/storefront/src/features/products/api/productApi.ts`

8. Not unwrapping `ApiResponse<T>` in frontend APIs
- Wrong: components receive nested response envelopes.
- Correct: use `transformResponse` to return `response.data`.
- Reference: `src/frontend/storefront/src/features/products/api/reviewsApi.ts`

9. Mixing server state into Redux slices
- Wrong: storing fetched API collections as long-lived slice state.
- Correct: keep server state in RTK Query cache; slices for UI state only.
- Reference: `src/frontend/storefront/src/features/cart/slices/cartSlice.ts`, `src/frontend/storefront/src/features/auth/slices/authSlice.ts`

10. Ignoring cancellation tokens in async backend methods
- Wrong: async methods without `CancellationToken`.
- Correct: add `CancellationToken cancellationToken = default` as last parameter.
- Reference: `src/backend/ECommerce.Application/Services/ProductService.cs`

11. Manual DTO construction everywhere
- Wrong: hand-mapping repeatedly in services.
- Correct: use AutoMapper where configured and keep mapping centralized.
- Reference: `src/backend/ECommerce.Application/Services/ProductService.cs`, `src/backend/ECommerce.Application/MappingProfile.cs`

## Usage Rule
Before implementing a feature or refactor, scan this file and explicitly check that no listed mistake is being introduced.

## Pattern Links
- Mistakes 1-3, 10-11: `.ai/backend/services.md`, `.ai/backend/repositories.md`, `.ai/backend/query-patterns.md`
- Mistakes 4-6: `.ai/backend/entities.md`, `.ai/backend/api-contracts.md`, `.ai/backend/error-handling.md`
- Mistakes 7-9: `.ai/frontend/api-integration.md`, `.ai/frontend/redux.md`, `.ai/frontend/type-safety.md`
- Full flow reference: `.ai/workflows/adding-feature.md`
