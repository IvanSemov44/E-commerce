# Phase 5, Step 4: Cutover

**Prerequisite**: Steps 1–3 complete. Characterization tests passing against the OLD service.

Rewrite `PromoCodesController` to dispatch via MediatR, then delete the old service, interface, and Core entity.

---

## Task 1: Pre-cutover verification

```bash
cd src/backend
dotnet test ECommerce.Tests --filter "FullyQualifiedName~PromoCodeCharacterizationTests" --logger "console;verbosity=normal"
# All must be green before proceeding
```

---

## Task 2: Rewrite PromoCodesController

Replace the contents of `src/backend/ECommerce.API/Controllers/PromoCodesController.cs` entirely:

```csharp
using ECommerce.API.ActionFilters;
using ECommerce.API.Helpers;
using ECommerce.Application.DTOs.Common;
using ECommerce.Promotions.Application.Commands;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.Queries;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/promo-codes")]
[Produces("application/json")]
[Tags("PromoCodes")]
public class PromoCodesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PromoCodesController> _logger;

    public PromoCodesController(IMediator mediator, ILogger<PromoCodesController> logger)
    {
        _mediator = mediator;
        _logger   = logger;
    }

    /// <summary>Get all active promo codes (Public — for storefront display).</summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveCodes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationRequestNormalizer.Normalize(page, pageSize);
        var result = await _mediator.Send(new GetActivePromoCodesQuery(page, pageSize), cancellationToken);
        return Ok(ApiResponse<PaginatedResult<PromoCodeDto>>.Ok(result.Value!, "Active promo codes retrieved successfully"));
    }

    /// <summary>Get all promo codes with pagination and filtering (Admin only).</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetAllPromoCodes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationRequestNormalizer.Normalize(page, pageSize);
        var result = await _mediator.Send(new GetPromoCodesQuery(page, pageSize, search, isActive), cancellationToken);
        return Ok(ApiResponse<PaginatedResult<PromoCodeDto>>.Ok(result.Value!, "Promo codes retrieved successfully"));
    }

    /// <summary>Get promo code by ID (Admin only).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetPromoCodeById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPromoCodeByIdQuery(id), cancellationToken);
        return MapResult(result, d => Ok(ApiResponse<PromoCodeDetailDto>.Ok(d, "Promo code retrieved successfully")));
    }

    /// <summary>Create a new promo code (Admin only).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    public async Task<IActionResult> CreatePromoCode(
        [FromBody] CreatePromoCodeRequestDto dto,
        CancellationToken cancellationToken)
    {
        var cmd = new CreatePromoCodeCommand(
            dto.Code,
            dto.DiscountType,
            dto.DiscountValue,
            dto.MinOrderAmount,
            dto.MaxDiscountAmount,
            dto.MaxUses,
            dto.StartDate,
            dto.EndDate,
            dto.IsActive);

        var result = await _mediator.Send(cmd, cancellationToken);

        if (!result.IsSuccess) return MapError(result.Error!);

        return CreatedAtAction(
            nameof(GetPromoCodeById),
            new { id = result.Value!.Id },
            ApiResponse<PromoCodeDetailDto>.Ok(result.Value, "Promo code created successfully"));
    }

    /// <summary>Update an existing promo code (Admin only).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    public async Task<IActionResult> UpdatePromoCode(
        Guid id,
        [FromBody] UpdatePromoCodeRequestDto dto,
        CancellationToken cancellationToken)
    {
        var cmd = new UpdatePromoCodeCommand(
            id,
            dto.Code,
            dto.DiscountType,
            dto.DiscountValue,
            dto.MinOrderAmount,
            dto.ClearMinOrderAmount,
            dto.MaxDiscountAmount,
            dto.ClearMaxDiscountAmount,
            dto.MaxUses,
            dto.ClearMaxUses,
            dto.StartDate,
            dto.EndDate,
            dto.ClearDates,
            dto.IsActive);

        var result = await _mediator.Send(cmd, cancellationToken);
        return MapResult(result, d => Ok(ApiResponse<PromoCodeDetailDto>.Ok(d, "Promo code updated successfully")));
    }

    /// <summary>Deactivate a promo code (Admin only) — soft-delete.</summary>
    [HttpPut("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeactivatePromoCode(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeactivatePromoCodeCommand(id), cancellationToken);
        return MapResult(result, _ => Ok(ApiResponse<object>.Ok(new object(), "Promo code deactivated successfully")));
    }

    /// <summary>Delete a promo code (Admin only) — hard delete.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeletePromoCode(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeletePromoCodeCommand(id), cancellationToken);
        return MapResult(result, _ => Ok(ApiResponse<object>.Ok(new object(), "Promo code deleted successfully")));
    }

    /// <summary>Validate a promo code for an order (Public — supports guest checkout).</summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    [ValidationFilter]
    public async Task<IActionResult> ValidatePromoCode(
        [FromBody] ValidatePromoCodeRequestDto request,
        CancellationToken cancellationToken)
    {
        // This query NEVER fails — invalid codes produce IsValid=false inside the DTO
        var result = await _mediator.Send(new ValidatePromoCodeQuery(request.Code, request.OrderAmount), cancellationToken);
        return Ok(ApiResponse<ValidatePromoCodeDto>.Ok(result.Value!, "Promo code validation completed"));
    }

    // ──────────────────────────────────────────────────────────
    // Error mapping
    // ──────────────────────────────────────────────────────────

    private IActionResult MapResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess) return onSuccess(result.Value!);
        return MapError(result.Error!);
    }

    private IActionResult MapResult(Result result, Func<Unit, IActionResult> onSuccess)
    {
        if (result.IsSuccess) return onSuccess(Unit.Value);
        return MapError(result.Error!);
    }

    private IActionResult MapError(DomainError error) => error.Code switch
    {
        "PROMO_CODE_NOT_FOUND"  => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),
        "DUPLICATE_PROMO_CODE"  => Conflict(ApiResponse<object>.Failure(error.Message, error.Code)),
        "CONCURRENCY_CONFLICT"  => Conflict(ApiResponse<object>.Failure(error.Message, error.Code)),
        "PROMO_NOT_VALID"       => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code)),
        "PROMO_MIN_ORDER"       => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code)),
        _                       => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code))
    };
}
```

### Request DTOs (add to `ECommerce.API/Models/` or inline in the controller file)

These replace the old `CreatePromoCodeDto` / `UpdatePromoCodeDto` from `ECommerce.Application.DTOs.PromoCodes`:

```csharp
// CreatePromoCodeRequestDto.cs
public class CreatePromoCodeRequestDto
{
    public string    Code             { get; set; } = null!;
    public string    DiscountType     { get; set; } = null!;
    public decimal   DiscountValue    { get; set; }
    public decimal?  MinOrderAmount   { get; set; }
    public decimal?  MaxDiscountAmount { get; set; }
    public int?      MaxUses          { get; set; }
    public DateTime? StartDate        { get; set; }
    public DateTime? EndDate          { get; set; }
    public bool      IsActive         { get; set; } = true;
}

// UpdatePromoCodeRequestDto.cs
public class UpdatePromoCodeRequestDto
{
    public string?   Code                 { get; set; }
    public string?   DiscountType         { get; set; }
    public decimal?  DiscountValue        { get; set; }
    public decimal?  MinOrderAmount       { get; set; }
    public bool      ClearMinOrderAmount  { get; set; }
    public decimal?  MaxDiscountAmount    { get; set; }
    public bool      ClearMaxDiscountAmount { get; set; }
    public int?      MaxUses              { get; set; }
    public bool      ClearMaxUses         { get; set; }
    public DateTime? StartDate            { get; set; }
    public DateTime? EndDate              { get; set; }
    public bool      ClearDates           { get; set; }
    public bool?     IsActive             { get; set; }
}

// ValidatePromoCodeRequestDto.cs  (can reuse existing one from Application.DTOs.PromoCodes)
public class ValidatePromoCodeRequestDto
{
    public string  Code        { get; set; } = null!;
    public decimal OrderAmount { get; set; }
}
```

**Note on validators**: Port the existing FluentValidation validators to reference the new request DTOs instead of the old ones. File: `ECommerce.API/Validators/PromoCodes/` or create new validators in the Application project.

---

## Task 3: Delete old files

After characterization tests pass with the new controller:

```bash
# Remove old service and interface
rm src/backend/ECommerce.Application/Services/PromoCodeService.cs
rm src/backend/ECommerce.Application/Interfaces/IPromoCodeService.cs

# Remove old DTOs (the new ones live in Promotions.Application)
rm src/backend/ECommerce.Application/DTOs/PromoCodes/CreatePromoCodeDto.cs
rm src/backend/ECommerce.Application/DTOs/PromoCodes/UpdatePromoCodeDto.cs
rm src/backend/ECommerce.Application/DTOs/PromoCodes/PromoCodeDto.cs
rm src/backend/ECommerce.Application/DTOs/PromoCodes/PromoCodeDetailDto.cs
rm src/backend/ECommerce.Application/DTOs/PromoCodes/ValidatePromoCodeDto.cs
rm src/backend/ECommerce.Application/DTOs/PromoCodes/PromoCodeQueryParameters.cs
# Keep ValidatePromoCodeRequestDto.cs if any validator still uses it, otherwise remove it too

# Remove old validators that reference deleted DTOs
rm src/backend/ECommerce.Application/Validators/PromoCodes/CreatePromoCodeDtoValidator.cs
rm src/backend/ECommerce.Application/Validators/PromoCodes/UpdatePromoCodeDtoValidator.cs
rm src/backend/ECommerce.Application/Validators/PromoCodes/PromoCodeQueryParametersValidator.cs
rm src/backend/ECommerce.Application/Validators/PromoCodes/ValidatePromoCodeRequestValidator.cs
```

Remove the DI registration from `Program.cs`:
```csharp
// Remove this line:
builder.Services.AddScoped<IPromoCodeService, PromoCodeService>();
```

Remove the old AutoMapper profile entry for `PromoCode` mapping (if one exists in `ECommerce.Application/Mappings/`):
```bash
grep -r "PromoCode" src/backend/ECommerce.Application/Mappings/ --include="*.cs"
# Remove any CreateMap<PromoCode, PromoCodeDto> etc.
```

---

## Task 4: Remove old Core entity and rename DbSet

After removing all references to `ECommerce.Core.Entities.PromoCode`:

1. Check for remaining references:
```bash
grep -r "Core.Entities.PromoCode\|ECommerce\.Core\.Entities\.PromoCode" src/backend --include="*.cs" | grep -v "\.git"
```

2. If zero results (except the entity file itself), delete it:
```bash
rm src/backend/ECommerce.Core/Entities/PromoCode.cs
```

3. In `AppDbContext.cs`, rename `PromoCodes2` → `PromoCodes`:
```csharp
// Before:
public DbSet<ECommerce.Promotions.Domain.Aggregates.PromoCode.PromoCode> PromoCodes2 { get; set; }

// After:
public DbSet<ECommerce.Promotions.Domain.Aggregates.PromoCode.PromoCode> PromoCodes { get; set; }
```

4. Update `PromoCodeRepository` to use `PromoCodes` instead of `PromoCodes2`.

5. Remove the old `PromoCodes` DbSet (now replaced).

---

## Task 5: Post-cutover verification

```bash
cd src/backend
dotnet build
# Must compile with zero errors

dotnet test ECommerce.Tests --filter "FullyQualifiedName~PromoCodeCharacterizationTests" --logger "console;verbosity=normal"
# All characterization tests must still pass

dotnet test ECommerce.Tests --filter "FullyQualifiedName~PromoCodesControllerTests" --logger "console;verbosity=normal"
# Existing tests must still pass
```

---

## Error code mapping reference

| Domain Error Code        | HTTP Status | Scenario |
|--------------------------|-------------|----------|
| `PROMO_CODE_NOT_FOUND`   | 404 Not Found | GetById, Update, Deactivate, Delete with unknown id |
| `DUPLICATE_PROMO_CODE`   | 409 Conflict | Create or Update with a code that already exists |
| `CONCURRENCY_CONFLICT`   | 409 Conflict | Two concurrent updates on the same row |
| `PROMO_NOT_VALID`        | 422 Unprocessable | RecordUsage on inactive/expired code (Phase 7) |
| `PROMO_MIN_ORDER`        | 422 Unprocessable | RecordUsage/Calculate below min order (Phase 7) |
| `CODE_EMPTY` / `CODE_LENGTH` / `CODE_CHARS` | 400 Bad Request | Invalid code string in create/update |
| `DISCOUNT_PERCENT_RANGE` / `DISCOUNT_AMOUNT_POSITIVE` | 400 Bad Request | Invalid discount value |
| `DATE_RANGE_INVALID`     | 400 Bad Request | StartDate >= EndDate |
| *(anything else)*        | 400 Bad Request | Fallthrough |

---

## Acceptance Criteria

- [ ] Controller compiles with zero errors
- [ ] All characterization tests pass against the new MediatR handlers
- [ ] All existing `PromoCodesControllerTests` pass
- [ ] `POST /validate` still returns 200 for valid and invalid codes
- [ ] `POST /api/promo-codes` still returns 201 Created with Location header
- [ ] `IPromoCodeService` and `PromoCodeService` deleted
- [ ] Old `ECommerce.Core.Entities.PromoCode` deleted
- [ ] `PromoCodes2` renamed to `PromoCodes` in AppDbContext
- [ ] `dotnet build` produces zero errors across the entire solution
