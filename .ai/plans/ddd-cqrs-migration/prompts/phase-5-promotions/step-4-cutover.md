# Phase 5, Step 4: Promotions Cutover

**Prerequisite**: Steps 1–3 complete, `dotnet build` clean, all existing tests pass.

---

## Pre-Cutover Verification

```bash
# 1. All tests pass (including characterization tests from step 0)
cd src/backend
dotnet test

# 2. Characterization tests specifically
dotnet test ECommerce.Tests/ECommerce.Tests.csproj \
    --filter "FullyQualifiedName~PromoCodeCharacterizationTests"

# 3. E2E tests (backend must be running against PostgreSQL)
cd src/frontend/storefront
npx playwright test api-promo-codes.spec.ts --reporter=list
```

All three must be green before proceeding.

---

## Error code → HTTP status mapping

| Error code | HTTP status |
|---|---|
| `PROMO_CODE_NOT_FOUND` | 404 Not Found |
| `DUPLICATE_PROMO_CODE` | 409 Conflict |
| `CONCURRENCY_CONFLICT` | 409 Conflict |
| `PROMO_NOT_VALID` / `PROMO_MIN_ORDER` | 422 Unprocessable |
| `CODE_EMPTY` / `CODE_LENGTH` / `CODE_CHARS` / `DATE_RANGE_INVALID` / `DISCOUNT_PERCENT_RANGE` / `DISCOUNT_AMOUNT_POSITIVE` | 400 Bad Request |
| `VALIDATION_FAILED` | 400 Bad Request |

---

## Task 1: Rewrite PromoCodesController

Keep all existing route paths, HTTP methods, and auth attributes. Replace `IPromoCodeService` with `IMediator`.

```csharp
using ECommerce.Promotions.Application.Commands.CreatePromoCode;
using ECommerce.Promotions.Application.Commands.DeactivatePromoCode;
using ECommerce.Promotions.Application.Commands.DeletePromoCode;
using ECommerce.Promotions.Application.Commands.UpdatePromoCode;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.Queries.GetActivePromoCodes;
using ECommerce.Promotions.Application.Queries.GetPromoCodeById;
using ECommerce.Promotions.Application.Queries.GetPromoCodes;
using ECommerce.Promotions.Application.Queries.ValidatePromoCode;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/promo-codes")]
public class PromoCodesController(IMediator _mediator) : ControllerBase
{
    // ── GET /api/promo-codes/active (anonymous, paginated) ─────────────────────
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActivePromoCodes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // pageSize clamped to 100 inside the handler
        var result = await _mediator.Send(new GetActivePromoCodesQuery(page, pageSize), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Active promo codes retrieved successfully"))
            : MapResult(result.GetErrorOrThrow());
    }

    // ── GET /api/promo-codes (admin list, filtered + paginated) ────────────────
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetPromoCodes(
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        [FromQuery] string? search   = null,
        [FromQuery] bool?   isActive = null,
        CancellationToken   ct       = default)
    {
        var result = await _mediator.Send(
            new GetPromoCodesQuery(page, pageSize, search, isActive), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Promo codes retrieved successfully"))
            : MapResult(result.GetErrorOrThrow());
    }

    // ── GET /api/promo-codes/{id} ──────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetPromoCodeById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPromoCodeByIdQuery(id), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Promo code retrieved successfully"))
            : MapResult(result.GetErrorOrThrow());
    }

    // ── POST /api/promo-codes ──────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    public async Task<IActionResult> CreatePromoCode(
        [FromBody] CreatePromoCodeDto dto, CancellationToken ct)
    {
        var command = new CreatePromoCodeCommand(
            dto.Code,
            dto.DiscountType,
            dto.DiscountValue,
            dto.MinOrderAmount,
            dto.MaxDiscountAmount,
            dto.MaxUses,
            dto.StartDate,
            dto.EndDate,
            dto.IsActive);

        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess)
            return MapResult(result.GetErrorOrThrow());

        var created = result.GetDataOrThrow();
        return CreatedAtAction(
            nameof(GetPromoCodeById),
            new { id = created.Id },
            ApiResponse<object>.Ok(created, "Promo code created successfully"));
    }

    // ── PUT /api/promo-codes/{id} ──────────────────────────────────────────────
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    public async Task<IActionResult> UpdatePromoCode(
        Guid id, [FromBody] UpdatePromoCodeDto dto, CancellationToken ct)
    {
        var command = new UpdatePromoCodeCommand(
            id,
            dto.Code,
            dto.DiscountType,
            dto.DiscountValue,
            dto.MinOrderAmount,
            dto.MaxDiscountAmount,
            dto.MaxUses,
            dto.MaxUses is not null, // MaxUsesPresent: if the field appears in JSON, the DTO has a value
            dto.StartDate,
            dto.EndDate,
            dto.StartDate is not null || dto.EndDate is not null, // DatesPresent
            dto.IsActive);

        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Promo code updated successfully"))
            : MapResult(result.GetErrorOrThrow());
    }

    // ── PUT /api/promo-codes/{id}/deactivate ───────────────────────────────────
    [HttpPut("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeactivatePromoCode(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeactivatePromoCodeCommand(id), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(new { }, "Promo code deactivated successfully"))
            : MapResult(result.GetErrorOrThrow());
    }

    // ── DELETE /api/promo-codes/{id} ───────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeletePromoCode(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeletePromoCodeCommand(id), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(new { }, "Promo code deleted successfully"))
            : MapResult(result.GetErrorOrThrow());
    }

    // ── POST /api/promo-codes/validate ─────────────────────────────────────────
    // IMPORTANT: This endpoint ALWAYS returns 200.
    // Invalid/not-found codes return { isValid: false } in data — never a 4xx error.
    [HttpPost("validate")]
    [AllowAnonymous]
    [ValidationFilter]
    public async Task<IActionResult> ValidatePromoCode(
        [FromBody] ValidatePromoCodeRequestDto dto, CancellationToken ct)
    {
        // ValidatePromoCodeQuery never fails — it always returns Result.Ok(dto) with IsValid=true/false
        var result = await _mediator.Send(new ValidatePromoCodeQuery(dto.Code, dto.OrderAmount), ct);
        return Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Promo code validated"));
    }

    private IActionResult MapResult(DomainError error) => error.Code switch
    {
        "PROMO_CODE_NOT_FOUND"
            => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),

        "DUPLICATE_PROMO_CODE" or "CONCURRENCY_CONFLICT"
            => Conflict(ApiResponse<object>.Failure(error.Message, error.Code)),

        "PROMO_NOT_VALID" or "PROMO_MIN_ORDER"
            => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code)),

        "CODE_EMPTY" or "CODE_LENGTH" or "CODE_CHARS"
        or "DATE_RANGE_INVALID" or "DISCOUNT_PERCENT_RANGE" or "DISCOUNT_AMOUNT_POSITIVE"
        or "VALIDATION_FAILED"
            => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code)),

        _ => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code))
    };
}
```

**Input DTOs for the controller** (add to `ECommerce.API/Models/` or use existing location):

```csharp
// CreatePromoCodeDto — used by POST /api/promo-codes
public class CreatePromoCodeDto
{
    [Required]
    public string Code { get; set; } = null!;

    [Required]
    public string DiscountType { get; set; } = null!;  // "Percentage" or "Fixed"

    [Required]
    public decimal DiscountValue { get; set; }

    public decimal?  MinOrderAmount    { get; set; }
    public decimal?  MaxDiscountAmount { get; set; }
    public int?      MaxUses           { get; set; }
    public DateTime? StartDate         { get; set; }
    public DateTime? EndDate           { get; set; }
    public bool      IsActive          { get; set; } = true;
}

// UpdatePromoCodeDto — used by PUT /api/promo-codes/{id} (all fields optional)
public class UpdatePromoCodeDto
{
    public string?   Code              { get; set; }
    public string?   DiscountType      { get; set; }
    public decimal?  DiscountValue     { get; set; }
    public decimal?  MinOrderAmount    { get; set; }
    public decimal?  MaxDiscountAmount { get; set; }
    public int?      MaxUses           { get; set; }
    public DateTime? StartDate         { get; set; }
    public DateTime? EndDate           { get; set; }
    public bool?     IsActive          { get; set; }
}

// ValidatePromoCodeRequestDto — used by POST /api/promo-codes/validate
public class ValidatePromoCodeRequestDto
{
    [Required]
    public string  Code        { get; set; } = null!;

    [Required]
    public decimal OrderAmount { get; set; }
}
```

---

## Task 2: Delete old files

Once the controller is updated and all tests pass:

```bash
rm src/backend/ECommerce.Application/Services/PromoCodeService.cs
rm src/backend/ECommerce.Application/Interfaces/IPromoCodeService.cs
```

Remove DI registration from `Program.cs` (or `ServiceCollectionExtensions.cs`):
```csharp
// REMOVE:
builder.Services.AddScoped<IPromoCodeService, PromoCodeService>();
```

Confirm no other references remain:
```bash
cd src/backend
grep -r "IPromoCodeService\|PromoCodeService" --include="*.cs" .
# Expected: zero results
```

---

## Task 3: Remove old Core entity and resolve DbSet conflict

Confirm the old `ECommerce.Core.Entities.PromoCode` has no remaining references:

```bash
grep -r "ECommerce\.Core\.Entities\.PromoCode\b" --include="*.cs" src/backend
# Expected: only the entity file itself and AppDbContext
```

If clear:
1. Delete `src/backend/ECommerce.Core/Entities/PromoCode.cs`
2. In `AppDbContext.cs`, remove the old DbSet:
   ```csharp
   // REMOVE the old Core entity DbSet (exact name may vary):
   // public DbSet<ECommerce.Core.Entities.PromoCode> PromoCodes => Set<...>();
   ```
3. Rename `PromoCodes2` back to `PromoCodes`:
   ```csharp
   // RENAME from PromoCodes2:
   public DbSet<ECommerce.Promotions.Domain.Aggregates.PromoCode.PromoCode> PromoCodes
       => Set<ECommerce.Promotions.Domain.Aggregates.PromoCode.PromoCode>();
   ```

---

## Post-Cutover Verification

```bash
cd src/backend
dotnet build
dotnet test

dotnet test ECommerce.Tests/ECommerce.Tests.csproj \
    --filter "FullyQualifiedName~PromoCodeCharacterizationTests"

cd src/frontend/storefront
npx playwright test api-promo-codes.spec.ts --reporter=list
```

---

## Acceptance Criteria

- [ ] `PromoCodesController` updated to use `IMediator`; route `api/promo-codes` preserved
- [ ] All 8 endpoints preserved with correct HTTP methods, routes, and auth
- [ ] `[AllowAnonymous]` on `GET /active` and `POST /validate`
- [ ] `[Authorize(Roles = "Admin,SuperAdmin")]` on all other endpoints
- [ ] `POST /api/promo-codes` returns `CreatedAtAction` → 201 with Location header
- [ ] `POST /api/promo-codes/validate` always returns 200 (query never returns a failure)
- [ ] `MapResult` covers all error codes with correct HTTP status codes
- [ ] Old `PromoCodeService`, `IPromoCodeService` deleted and removed from DI
- [ ] Old `ECommerce.Core.Entities.PromoCode` deleted
- [ ] DbSet renamed from `PromoCodes2` back to `PromoCodes`
- [ ] All characterization tests pass post-cutover
- [ ] All e2e tests pass post-cutover
- [ ] `dotnet build` clean — no references to old service types
