# Backend Validation Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Apply consistent validation across API, DTO, and business layers.

## 3-Layer Validation Model
1. Controller parameter bounds
- Examples: page/pageSize bounds, non-empty route ids.

2. FluentValidation for DTO shape
- Rules for required fields, ranges, formats, lengths.
- Validators live under `src/backend/ECommerce.Application/Validators/{Feature}/`.

3. Service business validation
- Ownership, uniqueness, state transitions, inventory constraints.
- Return `Result<T>.Fail(...)` with `ErrorCodes` for expected failures.

## Validation Filter
- `ValidationFilterAttribute` short-circuits invalid model state.
- Returns `422 UnprocessableEntity` with normalized error payload.
- Avoid duplicate model-state checks in each controller action.

## Real Code References
- Filter: `src/backend/ECommerce.API/ActionFilters/ValidationFilterAttribute.cs`
- Product validator: `src/backend/ECommerce.Application/Validators/Products/CreateProductDtoValidator.cs`
- Query validator: `src/backend/ECommerce.Application/Validators/Products/ProductQueryParametersValidator.cs`
- Service-side business validation examples: `src/backend/ECommerce.Application/Services/OrderService.cs`

## Common Mistakes
- Only validating DTO shape and skipping business constraints.
- Repeating model-state boilerplate instead of using validation filter.
- Returning generic errors without `ErrorCodes`.

## Checklist
- [ ] Controller performs only basic parameter sanity checks.
- [ ] DTO validator exists for request DTO.
- [ ] Service enforces business invariants.
- [ ] Failure uses `Result<T>` + semantic error code.
