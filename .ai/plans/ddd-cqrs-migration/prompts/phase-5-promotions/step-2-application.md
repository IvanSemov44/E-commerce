# Phase 5, Step 2: Promotions Application Project

**Prerequisite**: Step 1 (`ECommerce.Promotions.Domain`) is complete and `dotnet build` passes.

---

## Task: Create ECommerce.Promotions.Application Project

### 1. Create the project

```bash
cd src/backend
dotnet new classlib -n ECommerce.Promotions.Application -f net10.0 -o Promotions/ECommerce.Promotions.Application
dotnet sln ../../ECommerce.sln add Promotions/ECommerce.Promotions.Application/ECommerce.Promotions.Application.csproj

dotnet add Promotions/ECommerce.Promotions.Application/ECommerce.Promotions.Application.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
dotnet add Promotions/ECommerce.Promotions.Application/ECommerce.Promotions.Application.csproj \
    reference Promotions/ECommerce.Promotions.Domain/ECommerce.Promotions.Domain.csproj

dotnet add Promotions/ECommerce.Promotions.Application/ECommerce.Promotions.Application.csproj package MediatR

rm Promotions/ECommerce.Promotions.Application/Class1.cs
```

### 2. Create DependencyInjection.cs

**File: `Promotions/ECommerce.Promotions.Application/DependencyInjection.cs`**

```csharp
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Promotions.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPromotionsApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        return services;
    }
}
```

> Note: In practice `AddPromotionsInfrastructure` (step 3) calls `AddMediatR` from the Application assembly directly, so you may not need to call `AddPromotionsApplication` separately. Include this for completeness and testability.

### 3. Create DTOs

These must match the existing HTTP contract exactly (the characterization tests pin their shapes).

**File: `Promotions/ECommerce.Promotions.Application/DTOs/PromoCodeDto.cs`**

```csharp
namespace ECommerce.Promotions.Application.DTOs;

/// <summary>Lightweight DTO used in list responses and embedded in ValidatePromoCodeDto.</summary>
public record PromoCodeDto(
    Guid      Id,
    string    Code,
    string    DiscountType,
    decimal   DiscountValue,
    decimal?  MinOrderAmount,
    int?      MaxUses,
    int       UsedCount,
    bool      IsActive,
    DateTime? StartDate,
    DateTime? EndDate
);
```

**File: `Promotions/ECommerce.Promotions.Application/DTOs/PromoCodeDetailDto.cs`**

```csharp
namespace ECommerce.Promotions.Application.DTOs;

/// <summary>Full detail DTO returned by admin GET /api/promo-codes/{id} and POST (create).</summary>
public record PromoCodeDetailDto(
    Guid      Id,
    string    Code,
    string    DiscountType,
    decimal   DiscountValue,
    decimal?  MinOrderAmount,
    int?      MaxUses,
    int       UsedCount,
    bool      IsActive,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal?  MaxDiscountAmount,
    DateTime  CreatedAt,
    DateTime  UpdatedAt
);
```

**File: `Promotions/ECommerce.Promotions.Application/DTOs/ValidatePromoCodeDto.cs`**

```csharp
namespace ECommerce.Promotions.Application.DTOs;

/// <summary>
/// Response body for POST /api/promo-codes/validate.
/// IsValid=false does NOT mean an error — the endpoint always returns 200.
/// </summary>
public record ValidatePromoCodeDto(
    bool           IsValid,
    decimal        DiscountAmount,
    string?        Message,
    PromoCodeDto?  PromoCode
);
```

### 4. Create mapping extensions

**File: `Promotions/ECommerce.Promotions.Application/Mapping/PromotionsMappingExtensions.cs`**

```csharp
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Domain.Aggregates.PromoCode;

namespace ECommerce.Promotions.Application.Mapping;

public static class PromotionsMappingExtensions
{
    public static PromoCodeDetailDto ToDetailDto(this PromoCode p) => new(
        p.Id,
        p.Code.Value,
        p.Discount.Type.ToString(),
        p.Discount.Amount,
        p.MinimumOrderAmount,
        p.MaxUses,
        p.UsedCount,
        p.IsActive,
        p.ValidPeriod?.Start,
        p.ValidPeriod?.End,
        p.MaxDiscountAmount,
        p.CreatedAt,
        p.UpdatedAt
    );

    public static PromoCodeDto ToDto(this PromoCode p) => new(
        p.Id,
        p.Code.Value,
        p.Discount.Type.ToString(),
        p.Discount.Amount,
        p.MinimumOrderAmount,
        p.MaxUses,
        p.UsedCount,
        p.IsActive,
        p.ValidPeriod?.Start,
        p.ValidPeriod?.End
    );
}
```

> Assumes `PromoCode` inherits `BaseEntity` from SharedKernel which exposes `CreatedAt` and `UpdatedAt`.

---

### 5. Commands

#### CreatePromoCodeCommand

**File: `Promotions/ECommerce.Promotions.Application/Commands/CreatePromoCode/CreatePromoCodeCommand.cs`**

```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Promotions.Application.DTOs;

namespace ECommerce.Promotions.Application.Commands.CreatePromoCode;

public record CreatePromoCodeCommand(
    string    Code,
    string    DiscountType,   // "Percentage" or "Fixed"
    decimal   DiscountValue,
    decimal?  MinOrderAmount,
    decimal?  MaxDiscountAmount,
    int?      MaxUses,
    DateTime? StartDate,
    DateTime? EndDate,
    bool      IsActive
) : IRequest<Result<PromoCodeDetailDto>>, ITransactionalCommand;
```

**File: `Promotions/ECommerce.Promotions.Application/Commands/CreatePromoCode/CreatePromoCodeCommandHandler.cs`**

```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.Mapping;
using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.Promotions.Domain.ValueObjects;

namespace ECommerce.Promotions.Application.Commands.CreatePromoCode;

public class CreatePromoCodeCommandHandler(
    IPromoCodeRepository _repo,
    IUnitOfWork          _uow
) : IRequestHandler<CreatePromoCodeCommand, Result<PromoCodeDetailDto>>
{
    public async Task<Result<PromoCodeDetailDto>> Handle(
        CreatePromoCodeCommand command, CancellationToken ct)
    {
        // Duplicate check
        var existing = await _repo.GetByCodeAsync(command.Code.Trim().ToUpperInvariant(), ct);
        if (existing is not null)
            return Result<PromoCodeDetailDto>.Fail(PromotionsErrors.DuplicateCode);

        // Build discount value object
        var discountResult = BuildDiscount(command.DiscountType, command.DiscountValue);
        if (!discountResult.IsSuccess)
            return Result<PromoCodeDetailDto>.Fail(discountResult.GetErrorOrThrow());

        // Build optional date range
        DateRange? validPeriod = null;
        if (command.StartDate.HasValue && command.EndDate.HasValue)
        {
            var drResult = DateRange.Create(command.StartDate.Value, command.EndDate.Value);
            if (!drResult.IsSuccess)
                return Result<PromoCodeDetailDto>.Fail(drResult.GetErrorOrThrow());
            validPeriod = drResult.GetDataOrThrow();
        }

        var createResult = PromoCode.Create(
            command.Code,
            discountResult.GetDataOrThrow(),
            validPeriod,
            command.MaxUses,
            command.IsActive,
            command.MinOrderAmount,
            command.MaxDiscountAmount);

        if (!createResult.IsSuccess)
            return Result<PromoCodeDetailDto>.Fail(createResult.GetErrorOrThrow());

        var promoCode = createResult.GetDataOrThrow();

        await _repo.UpsertAsync(promoCode, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<PromoCodeDetailDto>.Ok(promoCode.ToDetailDto());
    }

    private static Result<DiscountValue> BuildDiscount(string discountType, decimal value)
    {
        return discountType.Trim().ToUpperInvariant() switch
        {
            "PERCENTAGE" => DiscountValue.Percentage(value),
            "FIXED"      => DiscountValue.Fixed(value),
            _            => Result<DiscountValue>.Fail(
                                new ECommerce.SharedKernel.Results.DomainError(
                                    "VALIDATION_FAILED",
                                    $"Unknown DiscountType '{discountType}'. Must be 'Percentage' or 'Fixed'."))
        };
    }
}
```

#### UpdatePromoCodeCommand

**File: `Promotions/ECommerce.Promotions.Application/Commands/UpdatePromoCode/UpdatePromoCodeCommand.cs`**

```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Promotions.Application.DTOs;

namespace ECommerce.Promotions.Application.Commands.UpdatePromoCode;

/// <summary>
/// Partial update — any null field means "keep existing value".
/// To explicitly clear MaxUses or ValidPeriod, pass the *Present flag as true with the value as null.
/// </summary>
public record UpdatePromoCodeCommand(
    Guid      Id,
    string?   Code,
    string?   DiscountType,
    decimal?  DiscountValue,
    decimal?  MinOrderAmount,
    decimal?  MaxDiscountAmount,
    int?      MaxUses,
    bool      MaxUsesPresent,     // true = caller explicitly sent MaxUses (even if null = clear)
    DateTime? StartDate,
    DateTime? EndDate,
    bool      DatesPresent,       // true = caller explicitly sent StartDate/EndDate (even if null = clear)
    bool?     IsActive
) : IRequest<Result<PromoCodeDetailDto>>, ITransactionalCommand;
```

**File: `Promotions/ECommerce.Promotions.Application/Commands/UpdatePromoCode/UpdatePromoCodeCommandHandler.cs`**

```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.Mapping;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.Promotions.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Promotions.Application.Commands.UpdatePromoCode;

public class UpdatePromoCodeCommandHandler(
    IPromoCodeRepository _repo,
    IUnitOfWork          _uow
) : IRequestHandler<UpdatePromoCodeCommand, Result<PromoCodeDetailDto>>
{
    public async Task<Result<PromoCodeDetailDto>> Handle(
        UpdatePromoCodeCommand command, CancellationToken ct)
    {
        var promoCode = await _repo.GetByIdAsync(command.Id, ct);
        if (promoCode is null)
            return Result<PromoCodeDetailDto>.Fail(PromotionsErrors.PromoNotFound);

        // Build new DiscountValue if either type or value was supplied
        DiscountValue? newDiscount = null;
        if (command.DiscountType is not null || command.DiscountValue.HasValue)
        {
            var type  = command.DiscountType ?? promoCode.Discount.Type.ToString();
            var value = command.DiscountValue ?? promoCode.Discount.Amount;

            var discountResult = type.Trim().ToUpperInvariant() switch
            {
                "PERCENTAGE" => DiscountValue.Percentage(value),
                "FIXED"      => DiscountValue.Fixed(value),
                _            => Result<DiscountValue>.Fail(
                                    new ECommerce.SharedKernel.Results.DomainError(
                                        "VALIDATION_FAILED",
                                        $"Unknown DiscountType '{type}'."))
            };

            if (!discountResult.IsSuccess)
                return Result<PromoCodeDetailDto>.Fail(discountResult.GetErrorOrThrow());

            newDiscount = discountResult.GetDataOrThrow();
        }

        // Build new DateRange if dates are explicitly present
        DateRange? newValidPeriod   = null;
        bool       validPeriodSet   = false;
        if (command.DatesPresent)
        {
            validPeriodSet = true;
            if (command.StartDate.HasValue && command.EndDate.HasValue)
            {
                var drResult = DateRange.Create(command.StartDate.Value, command.EndDate.Value);
                if (!drResult.IsSuccess)
                    return Result<PromoCodeDetailDto>.Fail(drResult.GetErrorOrThrow());
                newValidPeriod = drResult.GetDataOrThrow();
            }
            // else both null → clear ValidPeriod (newValidPeriod stays null)
        }

        var updateResult = promoCode.Update(
            command.Code,
            newDiscount,
            newValidPeriod,
            validPeriodSet,
            command.MaxUses,
            command.MaxUsesPresent,
            command.IsActive,
            command.MinOrderAmount,
            command.MaxDiscountAmount);

        if (!updateResult.IsSuccess)
            return Result<PromoCodeDetailDto>.Fail(updateResult.GetErrorOrThrow());

        try
        {
            await _repo.UpsertAsync(promoCode, ct);
            await _uow.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<PromoCodeDetailDto>.Fail(PromotionsErrors.ConcurrencyConflict);
        }

        return Result<PromoCodeDetailDto>.Ok(promoCode.ToDetailDto());
    }
}
```

#### DeactivatePromoCodeCommand

**File: `Promotions/ECommerce.Promotions.Application/Commands/DeactivatePromoCode/DeactivatePromoCodeCommand.cs`**

```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Promotions.Application.Commands.DeactivatePromoCode;

public record DeactivatePromoCodeCommand(Guid Id)
    : IRequest<Result>, ITransactionalCommand;
```

**File: `Promotions/ECommerce.Promotions.Application/Commands/DeactivatePromoCode/DeactivatePromoCodeCommandHandler.cs`**

```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Interfaces;

namespace ECommerce.Promotions.Application.Commands.DeactivatePromoCode;

public class DeactivatePromoCodeCommandHandler(
    IPromoCodeRepository _repo,
    IUnitOfWork          _uow
) : IRequestHandler<DeactivatePromoCodeCommand, Result>
{
    public async Task<Result> Handle(DeactivatePromoCodeCommand command, CancellationToken ct)
    {
        var promoCode = await _repo.GetByIdAsync(command.Id, ct);
        if (promoCode is null)
            return Result.Fail(PromotionsErrors.PromoNotFound);

        promoCode.Deactivate();

        await _repo.UpsertAsync(promoCode, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
```

#### DeletePromoCodeCommand

**File: `Promotions/ECommerce.Promotions.Application/Commands/DeletePromoCode/DeletePromoCodeCommand.cs`**

```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Promotions.Application.Commands.DeletePromoCode;

public record DeletePromoCodeCommand(Guid Id)
    : IRequest<Result>, ITransactionalCommand;
```

**File: `Promotions/ECommerce.Promotions.Application/Commands/DeletePromoCode/DeletePromoCodeCommandHandler.cs`**

```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Interfaces;

namespace ECommerce.Promotions.Application.Commands.DeletePromoCode;

public class DeletePromoCodeCommandHandler(
    IPromoCodeRepository _repo,
    IUnitOfWork          _uow
) : IRequestHandler<DeletePromoCodeCommand, Result>
{
    public async Task<Result> Handle(DeletePromoCodeCommand command, CancellationToken ct)
    {
        var promoCode = await _repo.GetByIdAsync(command.Id, ct);
        if (promoCode is null)
            return Result.Fail(PromotionsErrors.PromoNotFound);

        await _repo.DeleteAsync(promoCode, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
```

---

### 6. Queries

#### GetPromoCodesQuery (admin list)

**File: `Promotions/ECommerce.Promotions.Application/Queries/GetPromoCodes/GetPromoCodesQuery.cs`**

```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;

namespace ECommerce.Promotions.Application.Queries.GetPromoCodes;

public record GetPromoCodesQuery(
    int     Page,
    int     PageSize,
    string? Search,
    bool?   IsActive
) : IRequest<Result<PagedResult<PromoCodeDetailDto>>>;
```

**File: `Promotions/ECommerce.Promotions.Application/Queries/GetPromoCodes/GetPromoCodesQueryHandler.cs`**

```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.Mapping;
using ECommerce.Promotions.Domain.Interfaces;

namespace ECommerce.Promotions.Application.Queries.GetPromoCodes;

public class GetPromoCodesQueryHandler(IPromoCodeRepository _repo)
    : IRequestHandler<GetPromoCodesQuery, Result<PagedResult<PromoCodeDetailDto>>>
{
    public async Task<Result<PagedResult<PromoCodeDetailDto>>> Handle(
        GetPromoCodesQuery query, CancellationToken ct)
    {
        var (items, total) = await _repo.GetAllAsync(
            query.Page, query.PageSize, query.Search, query.IsActive, ct);

        var dtos = items.Select(p => p.ToDetailDto()).ToList();

        return Result<PagedResult<PromoCodeDetailDto>>.Ok(
            new PagedResult<PromoCodeDetailDto>(dtos, total, query.Page, query.PageSize));
    }
}
```

#### GetActivePromoCodesQuery (public list)

**File: `Promotions/ECommerce.Promotions.Application/Queries/GetActivePromoCodes/GetActivePromoCodesQuery.cs`**

```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;

namespace ECommerce.Promotions.Application.Queries.GetActivePromoCodes;

public record GetActivePromoCodesQuery(int Page, int PageSize)
    : IRequest<Result<PagedResult<PromoCodeDto>>>;
```

**File: `Promotions/ECommerce.Promotions.Application/Queries/GetActivePromoCodes/GetActivePromoCodesQueryHandler.cs`**

```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.Mapping;
using ECommerce.Promotions.Domain.Interfaces;

namespace ECommerce.Promotions.Application.Queries.GetActivePromoCodes;

public class GetActivePromoCodesQueryHandler(IPromoCodeRepository _repo)
    : IRequestHandler<GetActivePromoCodesQuery, Result<PagedResult<PromoCodeDto>>>
{
    private const int MaxPageSize = 100;

    public async Task<Result<PagedResult<PromoCodeDto>>> Handle(
        GetActivePromoCodesQuery query, CancellationToken ct)
    {
        var pageSize = Math.Min(query.PageSize, MaxPageSize);

        var (items, total) = await _repo.GetActiveAsync(query.Page, pageSize, ct);

        var dtos = items.Select(p => p.ToDto()).ToList();

        return Result<PagedResult<PromoCodeDto>>.Ok(
            new PagedResult<PromoCodeDto>(dtos, total, query.Page, pageSize));
    }
}
```

#### GetPromoCodeByIdQuery

**File: `Promotions/ECommerce.Promotions.Application/Queries/GetPromoCodeById/GetPromoCodeByIdQuery.cs`**

```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;

namespace ECommerce.Promotions.Application.Queries.GetPromoCodeById;

public record GetPromoCodeByIdQuery(Guid Id)
    : IRequest<Result<PromoCodeDetailDto>>;
```

**File: `Promotions/ECommerce.Promotions.Application/Queries/GetPromoCodeById/GetPromoCodeByIdQueryHandler.cs`**

```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.Mapping;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Interfaces;

namespace ECommerce.Promotions.Application.Queries.GetPromoCodeById;

public class GetPromoCodeByIdQueryHandler(IPromoCodeRepository _repo)
    : IRequestHandler<GetPromoCodeByIdQuery, Result<PromoCodeDetailDto>>
{
    public async Task<Result<PromoCodeDetailDto>> Handle(
        GetPromoCodeByIdQuery query, CancellationToken ct)
    {
        var promoCode = await _repo.GetByIdAsync(query.Id, ct);
        if (promoCode is null)
            return Result<PromoCodeDetailDto>.Fail(PromotionsErrors.PromoNotFound);

        return Result<PromoCodeDetailDto>.Ok(promoCode.ToDetailDto());
    }
}
```

#### ValidatePromoCodeQuery

**File: `Promotions/ECommerce.Promotions.Application/Queries/ValidatePromoCode/ValidatePromoCodeQuery.cs`**

```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;

namespace ECommerce.Promotions.Application.Queries.ValidatePromoCode;

/// <summary>
/// IMPORTANT: This query NEVER returns a domain error failure.
/// If the code is not found, invalid, or the order doesn't meet the minimum,
/// the result is Result.Ok with IsValid=false inside the DTO.
/// This preserves the existing API contract where POST /validate always returns 200.
/// </summary>
public record ValidatePromoCodeQuery(
    string  Code,
    decimal OrderAmount
) : IRequest<Result<ValidatePromoCodeDto>>;
```

**File: `Promotions/ECommerce.Promotions.Application/Queries/ValidatePromoCode/ValidatePromoCodeQueryHandler.cs`**

```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.Mapping;
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.Promotions.Domain.Services;

namespace ECommerce.Promotions.Application.Queries.ValidatePromoCode;

public class ValidatePromoCodeQueryHandler(
    IPromoCodeRepository _repo,
    DiscountCalculator   _calculator
) : IRequestHandler<ValidatePromoCodeQuery, Result<ValidatePromoCodeDto>>
{
    public async Task<Result<ValidatePromoCodeDto>> Handle(
        ValidatePromoCodeQuery query, CancellationToken ct)
    {
        // Normalise code for lookup (case-insensitive)
        var upperCode = query.Code?.Trim().ToUpperInvariant() ?? string.Empty;

        if (string.IsNullOrEmpty(upperCode))
            return Result<ValidatePromoCodeDto>.Ok(
                new ValidatePromoCodeDto(false, 0m, "Promo code is required.", null));

        var promoCode = await _repo.GetByCodeAsync(upperCode, ct);
        if (promoCode is null)
            return Result<ValidatePromoCodeDto>.Ok(
                new ValidatePromoCodeDto(false, 0m, "Promo code not found.", null));

        var calcResult = _calculator.Calculate(promoCode, query.OrderAmount, DateTime.UtcNow);
        if (!calcResult.IsSuccess)
        {
            // PROMO_NOT_VALID or PROMO_MIN_ORDER — return IsValid=false, not a failure
            return Result<ValidatePromoCodeDto>.Ok(
                new ValidatePromoCodeDto(false, 0m, calcResult.GetErrorOrThrow().Message, promoCode.ToDto()));
        }

        var calc = calcResult.GetDataOrThrow();
        return Result<ValidatePromoCodeDto>.Ok(
            new ValidatePromoCodeDto(true, calc.DiscountAmount, null, promoCode.ToDto()));
    }
}
```

### 7. PagedResult DTO

If `PagedResult<T>` is not already in SharedKernel, add it here:

**File: `Promotions/ECommerce.Promotions.Application/DTOs/PagedResult.cs`**

```csharp
namespace ECommerce.Promotions.Application.DTOs;

/// <summary>
/// Generic paginated response wrapper. Use only if not already defined in SharedKernel.
/// If SharedKernel already defines PagedResult<T>, delete this file and use that type instead.
/// </summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int              TotalCount,
    int              Page,
    int              PageSize
);
```

> Check `ECommerce.SharedKernel` first. If it already has `PagedResult<T>`, delete this file and reference the SharedKernel type in all handlers above.

### 8. Verify

```bash
cd src/backend
dotnet build Promotions/ECommerce.Promotions.Application/ECommerce.Promotions.Application.csproj
dotnet build
```

---

## Acceptance Criteria

- [ ] `ECommerce.Promotions.Application` project created and added to solution
- [ ] DTOs: `PromoCodeDto`, `PromoCodeDetailDto`, `ValidatePromoCodeDto` match existing HTTP contract shapes
- [ ] `PromotionsMappingExtensions.ToDetailDto` and `ToDto` implemented
- [ ] Commands: `CreatePromoCodeCommand`, `UpdatePromoCodeCommand`, `DeactivatePromoCodeCommand`, `DeletePromoCodeCommand` — all implement `ITransactionalCommand` and inject `IUnitOfWork`
- [ ] Queries: `GetPromoCodesQuery`, `GetActivePromoCodesQuery`, `GetPromoCodeByIdQuery`, `ValidatePromoCodeQuery`
- [ ] `GetActivePromoCodesQueryHandler` clamps pageSize to 100
- [ ] `ValidatePromoCodeQueryHandler` NEVER returns a domain error failure — always returns `Result.Ok(dto)` with `IsValid=false` in the DTO when code is not found, invalid, or min order not met
- [ ] `UpdatePromoCodeCommandHandler` catches `DbUpdateConcurrencyException` and returns `ConcurrencyConflict` error
- [ ] `SaveChangesAsync` never called on failure paths
- [ ] `dotnet build` passes
