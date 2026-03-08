# Code Examples Reference

Updated: 2026-03-08
Owner: @ivans

## Backend Examples
- Service layer with `Result<T>`:
  - `src/backend/ECommerce.Application/Services/ProductService.cs`
  - `src/backend/ECommerce.Application/Services/OrderService.cs`
- Repository pattern:
  - `src/backend/ECommerce.Infrastructure/Repositories/Repository.cs`
  - `src/backend/ECommerce.Infrastructure/Repositories/ProductRepository.cs`
- Unit of Work:
  - `src/backend/ECommerce.Infrastructure/UnitOfWork.cs`
- Thin controllers:
  - `src/backend/ECommerce.API/Controllers/ProductsController.cs`

## Frontend Examples
- Base API + auth retry path:
  - `src/frontend/storefront/src/shared/lib/api/baseApi.ts`
- API error handling hook:
  - `src/frontend/storefront/src/shared/hooks/useApiErrorHandler.ts`
- Typed form + Zod:
  - `src/frontend/storefront/src/shared/hooks/useForm.ts`
  - `src/frontend/storefront/src/shared/lib/utils/zodValidate.ts`
- Slice examples:
  - `src/frontend/storefront/src/features/auth/slices/authSlice.ts`
  - `src/frontend/storefront/src/features/cart/slices/cartSlice.ts`
