# Backend Controllers Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Define controller responsibilities and response conventions.

## Controller Responsibilities
- Accept and validate transport-layer input.
- Call application services.
- Map service outcomes to HTTP status + `ApiResponse<T>`.
- Do not implement business rules in controllers.

## Required Patterns
1. Keep endpoints thin.
2. Use `[ValidationFilter]` for DTO write endpoints.
3. Use `[ProducesResponseType]` coverage for expected statuses.
4. Use auth attributes explicitly (`[Authorize]`, `[AllowAnonymous]`).
5. Read user context through current-user service when needed.

## Response Shape
- Success: `ApiResponse<T>.Ok(...)`
- Failure: `ApiResponse<T>.Failure(...)`

## Real Code References
- Validation filter: `src/backend/ECommerce.API/ActionFilters/ValidationFilterAttribute.cs`
- Example controller (products): `src/backend/ECommerce.API/Controllers/ProductsController.cs`
- Example controller (orders): `src/backend/ECommerce.API/Controllers/OrdersController.cs`
- Example controller (auth): `src/backend/ECommerce.API/Controllers/AuthController.cs`

## Common Mistakes
- Business logic inside controller methods.
- Returning entities directly instead of DTOs.
- Missing `ProducesResponseType` for failure paths.
- Missing `ValidationFilter` for write DTOs.

## Checklist
- [ ] Endpoint returns `ApiResponse<T>` envelope.
- [ ] Controller delegates business decisions to service.
- [ ] Validation and auth attributes are explicit.
- [ ] Cancellation token is passed through to service.
