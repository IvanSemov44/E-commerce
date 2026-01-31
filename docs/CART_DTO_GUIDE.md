# Cart DTO Guidelines

This document describes conventions and best practices for working with the cart-related DTOs in the application (files under `src/backend/ECommerce.Application/DTOs/Cart`). It covers naming, placement, validation, mapping, testing, and when to split DTOs into separate files.

## Purpose and Principles
- Keep DTOs simple: properties only, no business logic.
- Group related DTOs by feature (Cart) to improve discoverability.
- Use DTOs for API surface: never return EF entities from controllers.

## File Organization
- Preferred: group small, tightly related DTOs in a single file: `CartDtos.cs`.
- When to split into one-class-per-file:
  - File grows beyond ~200 lines.
  - DTOs are widely reused in multiple features or projects.
  - You need to attach dedicated validators or documentation per DTO.

Recommended path: `src/backend/ECommerce.Application/DTOs/Cart/CartDtos.cs`

## Naming Conventions
- Use `*Dto` suffix for response/read DTOs: `CartDto`, `CartItemDto`.
- Use `Create*Dto` / `Update*Dto` where applicable for write operations.
- Use `*Request` for action-specific request payloads if you prefer (e.g., `CheckoutRequest`).

Current cart DTOs (good example):

- `CartDto` — response containing cart items and totals.
- `CartItemDto` — item information used in cart responses.
- `AddToCartDto` — request to add an item to the cart.
- `UpdateCartItemDto` — request to modify quantity.

## Validation
- Add FluentValidation validators for all input DTOs (requests): `AddToCartDto`, `UpdateCartItemDto`.
- Example rules:
  - `ProductId` required and must be a valid GUID.
  - `Quantity` required, integer >= 1.

Example validator (summary):

```csharp
public class AddToCartDtoValidator : AbstractValidator<AddToCartDto>
{
    public AddToCartDtoValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().Must(BeAValidGuid);
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(1);
    }
}
```

Register validators in `Program.cs` (API project):

```csharp
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<AddToCartDtoValidator>();
```

## AutoMapper
- Centralize mappings in `ECommerce.Application/MappingProfile.cs`.
- Add explicit maps for `Cart`/`CartItem` entities to `CartDto`/`CartItemDto`.
- Prefer mapping at query/projection time when returning lists to avoid loading extra data.

Example mapping snippet:

```csharp
CreateMap<Cart, CartDto>()
    .ForMember(d => d.Items, opt => opt.MapFrom(src => src.Items));

CreateMap<CartItem, CartItemDto>();
```

If mapping requires extra data (e.g., product name from another repo), do that in the service (or create a custom projection) rather than adding logic in DTOs.

## Services & Controllers
- Services should accept request DTOs (or primitives) and return DTOs.
- Do not return EF entities from controllers — always map to DTOs.
- Example controller action:

```csharp
[HttpPost("add")]
public async Task<IActionResult> Add([FromBody] AddToCartDto dto)
{
    var result = await _cartService.AddToCartAsync(userId, dto);
    return Ok(ApiResponse<CartDto>.Ok(result));
}
```

## Testing
- Unit test validators to ensure invalid inputs are rejected.
- Add AutoMapper configuration tests (spot-checks or `AssertConfigurationIsValid()` in a test project that can construct required profiles).
- Mock `IMapper` in service unit tests if constructing real mapper is expensive or requires external data.

## Serialization & Performance
- Keep DTOs compact for collection endpoints (avoid embedding large objects).
- Consider adding a lightweight `CartSummaryDto` for list endpoints.

## Documentation
- Add short XML comments to DTO classes and properties to improve generated API docs.

Example:

```csharp
/// <summary>
/// Request to add a product to the cart.
/// </summary>
public class AddToCartDto { ... }
```

## Migration Checklist (when changing or moving DTOs)
- Update `using` statements in controllers and services.
- Update AutoMapper mappings.
- Update unit tests and mocks.
- Run `dotnet build` and `dotnet test`.

## Suggested Quick Actions
1. Add validators for `AddToCartDto` and `UpdateCartItemDto`.
2. Add AutoMapper maps for `Cart` → `CartDto` if not present.
3. Add a simple AutoMapper test that instantiates `MappingProfile` and runs `AssertConfigurationIsValid()` in the test project.

---
If you want, I can:
- create the validator files and register them, or
- split the DTOs into individual files and add XML comments.
Tell me which and I'll implement it.
