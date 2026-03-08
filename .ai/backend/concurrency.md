# Backend Concurrency Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Keep writes safe under concurrent updates and return predictable conflict behavior.

## Core Rules
1. Frequently updated entities must carry concurrency token support.
2. Catch and map `DbUpdateConcurrencyException` consistently.
3. Return conflict semantics for stale/competing writes.
4. Keep retry/merge policy explicit per operation.

## Real Code References
- Concurrency-capable entities:
  - `src/backend/ECommerce.Core/Entities/Order.cs`
  - `src/backend/ECommerce.Core/Entities/Cart.cs`
  - `src/backend/ECommerce.Core/Entities/Product.cs`
  - `src/backend/ECommerce.Core/Entities/PromoCode.cs`
  - `src/backend/ECommerce.Core/Entities/User.cs`
- Shared middleware mapping:
  - `src/backend/ECommerce.API/Middleware/GlobalExceptionMiddleware.cs`
- Service-level handling examples:
  - `src/backend/ECommerce.Application/Services/OrderService.cs`
  - `src/backend/ECommerce.Application/Services/PaymentService.cs`
  - `src/backend/ECommerce.Application/Services/ReviewService.cs`

## Entity Coverage Matrix
- `Order`: concurrency token present.
- `Cart`: concurrency token present.
- `Product`: concurrency token present.
- `PromoCode`: concurrency token present.
- `User`: concurrency token present.

## Common Mistakes
- Missing conflict handling in update-heavy services.
- Returning generic 500 on known concurrency conflict.
- Treating concurrency conflicts as validation errors.

## Checklist
- [ ] Entity has concurrency token where required.
- [ ] Service catches and handles `DbUpdateConcurrencyException` where appropriate.
- [ ] API conflict response is consistent and actionable.
