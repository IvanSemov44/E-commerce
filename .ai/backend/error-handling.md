# Backend Error Handling Rules

Updated: 2026-03-08
Owner: @ivans

## Purpose
Define one consistent error-handling model for services, controllers, and middleware.

## Core Rules
1. Business logic returns `Result<T>`; do not throw for expected domain outcomes.
2. Use centralized error codes from `ErrorCodes`.
3. Controllers map `Result<T>` to HTTP responses and `ApiResponse<T>`.
4. Global middleware handles unexpected infrastructure exceptions.
5. Validation failures should be handled by validation filters and returned in standard shape.

## Canonical Implementation
- Result model:
  - `src/backend/ECommerce.Core/Results/Result.cs`
- Error code constants:
  - `src/backend/ECommerce.Core/Constants/ErrorCodes.cs`
- Global exception handling:
  - `src/backend/ECommerce.API/Middleware/GlobalExceptionMiddleware.cs`
- Validation filter:
  - `src/backend/ECommerce.API/ActionFilters/ValidationFilterAttribute.cs`

## Representative Usage
- Service returning failures/success:
  - `src/backend/ECommerce.Application/Services/WishlistService.cs`
  - `src/backend/ECommerce.Application/Services/OrderService.cs`
- Controller mapping Result to HTTP:
  - `src/backend/ECommerce.API/Controllers/WishlistController.cs`
  - `src/backend/ECommerce.API/Controllers/ProductsController.cs`

## Practical Mapping
- `Result<T>.Success` -> 200/201 with `ApiResponse<T>.Ok(...)`
- `Result<T>.Failure` -> 400/404/409/422 based on failure code/context
- Unexpected exceptions -> middleware maps to standard failure response

## Common Failure Modes
- Throwing exceptions in services for normal business failures.
- Returning raw entities from controllers.
- Using hardcoded strings instead of `ErrorCodes` constants.
- Duplicating validation in service when validator + filter already covers request shape.

## Quick Review Checklist
- [ ] Service method returns `Result<T>`.
- [ ] Failure paths use `ErrorCodes`.
- [ ] Controller handles both success and failure explicitly.
- [ ] Endpoint response remains inside `ApiResponse<T>` envelope.
- [ ] Validation errors are normalized by `ValidationFilterAttribute`.
