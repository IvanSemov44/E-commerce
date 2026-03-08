# Workflow: Adding a New Feature

Updated: 2026-03-08
Owner: @ivans

## Purpose
Implement features in a consistent order so backend and frontend stay aligned with existing project patterns.

## Backend Sequence
1. Core: add/update entity and enums.
2. Application: add DTOs and validators.
3. Core/Infrastructure: add repository contract + implementation if custom query is needed.
4. Application: implement service with `IUnitOfWork`, `IMapper`, `ILogger<T>` and return `Result<T>`.
5. API: add controller endpoint with `[ValidationFilter]` and `ProducesResponseType`.
6. Tests: add unit tests + integration tests.

## Frontend Sequence
1. Add/extend feature API via `baseApi.injectEndpoints(...)`.
2. Add or update slice only for UI state (server state stays in RTK Query).
3. Build/extend page and feature components.
4. Handle loading/error states (`isLoading`, `useApiErrorHandler`, query result checks).
5. Add/update tests (slice/component/e2e as needed).

## Real Code References
- Entity examples:
  - `src/backend/ECommerce.Core/Entities/Product.cs`
  - `src/backend/ECommerce.Core/Entities/Order.cs`
- DTO examples:
  - `src/backend/ECommerce.Application/DTOs/Products/ProductDto.cs`
  - `src/backend/ECommerce.Application/DTOs/Products/CreateProductDto.cs`
  - `src/backend/ECommerce.Application/DTOs/Products/ProductDetailDto.cs`
- Validator examples:
  - `src/backend/ECommerce.Application/Validators/Products/CreateProductDtoValidator.cs`
  - `src/backend/ECommerce.Application/Validators/Products/ProductQueryParametersValidator.cs`
- Repository examples:
  - `src/backend/ECommerce.Infrastructure/Repositories/ProductRepository.cs`
  - `src/backend/ECommerce.Infrastructure/Repositories/OrderRepository.cs`
- Service examples:
  - `src/backend/ECommerce.Application/Services/ProductService.cs`
  - `src/backend/ECommerce.Application/Services/OrderService.cs`
- Controller examples:
  - `src/backend/ECommerce.API/Controllers/ProductsController.cs`
  - `src/backend/ECommerce.API/Controllers/OrdersController.cs`
- Unit/Integration tests:
  - `src/backend/ECommerce.Tests/Unit/Services/ProductServiceTests.cs`
  - `src/backend/ECommerce.Tests/Integration/ProductsControllerTests.cs`

- Frontend API examples:
  - `src/frontend/storefront/src/shared/lib/api/baseApi.ts`
  - `src/frontend/storefront/src/features/products/api/productApi.ts`
  - `src/frontend/storefront/src/features/orders/api/ordersApi.ts`
- Frontend state examples:
  - `src/frontend/storefront/src/features/auth/slices/authSlice.ts`
  - `src/frontend/storefront/src/features/cart/slices/cartSlice.ts`
- Frontend page examples:
  - `src/frontend/storefront/src/features/auth/pages/LoginPage.tsx`
  - `src/frontend/storefront/src/features/orders/pages/OrderDetailPage/OrderDetailPage.tsx`
- Frontend test examples:
  - `src/frontend/storefront/src/features/auth/slices/__tests__/authSlice.test.ts`
  - `src/frontend/storefront/src/shared/hooks/__tests__/useApiErrorHandler.test.ts`

## Checklist
- [ ] Service returns `Result<T>` for business outcomes.
- [ ] Repository does not call `SaveChangesAsync`.
- [ ] Controller stays thin and wraps output with `ApiResponse<T>`.
- [ ] Validator exists for write/query DTOs.
- [ ] Async methods include `CancellationToken cancellationToken = default`.
- [ ] Backend unit and integration tests added/updated.
- [ ] Frontend API uses RTK Query, not direct `fetch` in components.
