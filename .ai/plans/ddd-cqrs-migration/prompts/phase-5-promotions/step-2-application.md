# Phase 5, Step 2: Application Project

**Prerequisite**: Step 1 (Domain) complete and building.

Create `ECommerce.Promotions.Application` — Commands, Queries, Handlers, DTOs, and mappings. No EF, no HTTP, no infrastructure details.

---

## Task 1: Create the project

```bash
cd src/backend
dotnet new classlib -n ECommerce.Promotions.Application -o ECommerce.Promotions.Application
dotnet sln ECommerce.sln add ECommerce.Promotions.Application/ECommerce.Promotions.Application.csproj
dotnet add ECommerce.Promotions.Application/ECommerce.Promotions.Application.csproj reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
dotnet add ECommerce.Promotions.Application/ECommerce.Promotions.Application.csproj reference ECommerce.Promotions.Domain/ECommerce.Promotions.Domain.csproj
dotnet add ECommerce.Promotions.Application/ECommerce.Promotions.Application.csproj package MediatR
rm ECommerce.Promotions.Application/Class1.cs
```

---

## Task 2: DependencyInjection

`ECommerce.Promotions.Application/DependencyInjection.cs`

```csharp
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

---

## Task 3: DTOs

These must exactly match the existing API contract so the controller and characterization tests work unchanged.

### `ECommerce.Promotions.Application/DTOs/PromoCodeDto.cs`
```csharp
namespace ECommerce.Promotions.Application.DTOs;

public record PromoCodeDto
{
    public Guid      Id            { get; init; }
    public string    Code          { get; init; } = null!;
    public string    DiscountType  { get; init; } = null!;
    public decimal   DiscountValue { get; init; }
    public decimal?  MinOrderAmount { get; init; }
    public int?      MaxUses       { get; init; }
    public int       UsedCount     { get; init; }
    public bool      IsActive      { get; init; }
    public DateTime? StartDate     { get; init; }
    public DateTime? EndDate       { get; init; }
}
```

### `ECommerce.Promotions.Application/DTOs/PromoCodeDetailDto.cs`
```csharp
namespace ECommerce.Promotions.Application.DTOs;

public record PromoCodeDetailDto
{
    public Guid      Id               { get; init; }
    public string    Code             { get; init; } = null!;
    public string    DiscountType     { get; init; } = null!;
    public decimal   DiscountValue    { get; init; }
    public decimal?  MinOrderAmount   { get; init; }
    public decimal?  MaxDiscountAmount { get; init; }
    public int?      MaxUses          { get; init; }
    public int       UsedCount        { get; init; }
    public bool      IsActive         { get; init; }
    public DateTime? StartDate        { get; init; }
    public DateTime? EndDate          { get; init; }
    public DateTime  CreatedAt        { get; init; }
    public DateTime  UpdatedAt        { get; init; }
}
```

### `ECommerce.Promotions.Application/DTOs/ValidatePromoCodeDto.cs`
```csharp
namespace ECommerce.Promotions.Application.DTOs;

public record ValidatePromoCodeDto
{
    public bool         IsValid        { get; init; }
    public string?      Message        { get; init; }
    public decimal      DiscountAmount { get; init; }
    public PromoCodeDto? PromoCode     { get; init; }
}
```

---

## Task 4: Mapping Extensions

`ECommerce.Promotions.Application/Mappings/PromotionsMappingExtensions.cs`

```csharp
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Domain.Aggregates.PromoCode;

namespace ECommerce.Promotions.Application.Mappings;

public static class PromotionsMappingExtensions
{
    public static PromoCodeDto ToDto(this PromoCode p) => new()
    {
        Id             = p.Id,
        Code           = p.Code.Value,
        DiscountType   = p.Discount.Type.ToString(),
        DiscountValue  = p.Discount.Amount,
        MinOrderAmount = p.MinimumOrderAmount,
        MaxUses        = p.MaxUses,
        UsedCount      = p.UsedCount,
        IsActive       = p.IsActive,
        StartDate      = p.ValidPeriod?.Start,
        EndDate        = p.ValidPeriod?.End
    };

    public static PromoCodeDetailDto ToDetailDto(this PromoCode p) => new()
    {
        Id                = p.Id,
        Code              = p.Code.Value,
        DiscountType      = p.Discount.Type.ToString(),
        DiscountValue     = p.Discount.Amount,
        MinOrderAmount    = p.MinimumOrderAmount,
        MaxDiscountAmount = p.MaxDiscountAmount,
        MaxUses           = p.MaxUses,
        UsedCount         = p.UsedCount,
        IsActive          = p.IsActive,
        StartDate         = p.ValidPeriod?.Start,
        EndDate           = p.ValidPeriod?.End,
        CreatedAt         = p.CreatedAt,
        UpdatedAt         = p.UpdatedAt
    };
}
```

---

## Task 5: Commands

### `ECommerce.Promotions.Application/Commands/CreatePromoCodeCommand.cs`
```csharp
using ECommerce.SharedKernel.CQRS;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.Mappings;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.Promotions.Domain.Services;
using ECommerce.Promotions.Domain.ValueObjects;
using MediatR;

namespace ECommerce.Promotions.Application.Commands;

public record CreatePromoCodeCommand(
    string    Code,
    string    DiscountType,
    decimal   DiscountValue,
    decimal?  MinOrderAmount,
    decimal?  MaxDiscountAmount,
    int?      MaxUses,
    DateTime? StartDate,
    DateTime? EndDate,
    bool      IsActive = true
) : IRequest<Result<PromoCodeDetailDto>>, ITransactionalCommand;

public class CreatePromoCodeCommandHandler : IRequestHandler<CreatePromoCodeCommand, Result<PromoCodeDetailDto>>
{
    private readonly IPromoCodeRepository _repo;
    private readonly IUnitOfWork          _uow;

    public CreatePromoCodeCommandHandler(IPromoCodeRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow  = uow;
    }

    public async Task<Result<PromoCodeDetailDto>> Handle(CreatePromoCodeCommand cmd, CancellationToken cancellationToken)
    {
        // 1. Build value objects
        var codeResult = PromoCodeString.Create(cmd.Code);
        if (!codeResult.IsSuccess) return Result<PromoCodeDetailDto>.Fail(codeResult.Error!);

        var discountResult = BuildDiscount(cmd.DiscountType, cmd.DiscountValue);
        if (!discountResult.IsSuccess) return Result<PromoCodeDetailDto>.Fail(discountResult.Error!);

        Domain.ValueObjects.DateRange? validPeriod = null;
        if (cmd.StartDate.HasValue && cmd.EndDate.HasValue)
        {
            var rangeResult = Domain.ValueObjects.DateRange.Create(cmd.StartDate.Value, cmd.EndDate.Value);
            if (!rangeResult.IsSuccess) return Result<PromoCodeDetailDto>.Fail(rangeResult.Error!);
            validPeriod = rangeResult.Value;
        }

        // 2. Duplicate check
        var existing = await _repo.GetByCodeAsync(codeResult.Value!.Value, cancellationToken);
        if (existing is not null)
            return Result<PromoCodeDetailDto>.Fail(PromotionsErrors.DuplicateCode);

        // 3. Create aggregate
        var promo = Domain.Aggregates.PromoCode.PromoCode.Create(
            codeResult.Value!,
            discountResult.Value!,
            validPeriod,
            cmd.MaxUses,
            cmd.MinOrderAmount,
            cmd.MaxDiscountAmount);

        await _repo.UpsertAsync(promo, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<PromoCodeDetailDto>.Ok(promo.ToDetailDto());
    }

    private static Result<DiscountValue> BuildDiscount(string discountType, decimal amount)
    {
        return discountType.Trim().ToLowerInvariant() switch
        {
            "percentage" => DiscountValue.Percentage(amount),
            "fixed"      => DiscountValue.Fixed(amount),
            _            => Result<DiscountValue>.Fail(new DomainError("INVALID_DISCOUNT_TYPE",
                                $"Unknown discount type '{discountType}'. Use 'Percentage' or 'Fixed'"))
        };
    }
}
```

### `ECommerce.Promotions.Application/Commands/UpdatePromoCodeCommand.cs`
```csharp
using ECommerce.SharedKernel.CQRS;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.Mappings;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.Promotions.Domain.ValueObjects;
using MediatR;

namespace ECommerce.Promotions.Application.Commands;

/// <summary>All fields are nullable — only non-null values are applied to the aggregate.</summary>
public record UpdatePromoCodeCommand(
    Guid      Id,
    string?   Code,
    string?   DiscountType,
    decimal?  DiscountValue,
    decimal?  MinOrderAmount,
    bool      ClearMinOrderAmount,
    decimal?  MaxDiscountAmount,
    bool      ClearMaxDiscountAmount,
    int?      MaxUses,
    bool      ClearMaxUses,
    DateTime? StartDate,
    DateTime? EndDate,
    bool      ClearDates,
    bool?     IsActive
) : IRequest<Result<PromoCodeDetailDto>>, ITransactionalCommand;

public class UpdatePromoCodeCommandHandler : IRequestHandler<UpdatePromoCodeCommand, Result<PromoCodeDetailDto>>
{
    private readonly IPromoCodeRepository _repo;
    private readonly IUnitOfWork          _uow;

    public UpdatePromoCodeCommandHandler(IPromoCodeRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow  = uow;
    }

    public async Task<Result<PromoCodeDetailDto>> Handle(UpdatePromoCodeCommand cmd, CancellationToken cancellationToken)
    {
        var promo = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (promo is null) return Result<PromoCodeDetailDto>.Fail(PromotionsErrors.PromoNotFound);

        // Build optional value objects only if fields provided
        PromoCodeString? newCode = null;
        if (cmd.Code is not null)
        {
            var r = PromoCodeString.Create(cmd.Code);
            if (!r.IsSuccess) return Result<PromoCodeDetailDto>.Fail(r.Error!);

            // Duplicate check (only if code is actually changing)
            if (r.Value!.Value != promo.Code.Value)
            {
                var dup = await _repo.GetByCodeAsync(r.Value.Value, cancellationToken);
                if (dup is not null && dup.Id != promo.Id)
                    return Result<PromoCodeDetailDto>.Fail(PromotionsErrors.DuplicateCode);
            }
            newCode = r.Value;
        }

        DiscountValue? newDiscount = null;
        if (cmd.DiscountType is not null || cmd.DiscountValue.HasValue)
        {
            var type   = cmd.DiscountType ?? promo.Discount.Type.ToString();
            var amount = cmd.DiscountValue ?? promo.Discount.Amount;
            var r = BuildDiscount(type, amount);
            if (!r.IsSuccess) return Result<PromoCodeDetailDto>.Fail(r.Error!);
            newDiscount = r.Value;
        }

        Domain.ValueObjects.DateRange? newPeriod = null;
        if (!cmd.ClearDates && (cmd.StartDate.HasValue || cmd.EndDate.HasValue))
        {
            var start = cmd.StartDate ?? promo.ValidPeriod?.Start;
            var end   = cmd.EndDate   ?? promo.ValidPeriod?.End;
            if (start.HasValue && end.HasValue)
            {
                var r = Domain.ValueObjects.DateRange.Create(start.Value, end.Value);
                if (!r.IsSuccess) return Result<PromoCodeDetailDto>.Fail(r.Error!);
                newPeriod = r.Value;
            }
        }

        promo.Update(
            code:                   newCode,
            discount:               newDiscount,
            validPeriod:            newPeriod,
            clearValidPeriod:       cmd.ClearDates,
            maxUses:                cmd.MaxUses,
            clearMaxUses:           cmd.ClearMaxUses,
            minimumOrderAmount:     cmd.MinOrderAmount,
            clearMinimumOrderAmount: cmd.ClearMinOrderAmount,
            maxDiscountAmount:      cmd.MaxDiscountAmount,
            clearMaxDiscountAmount: cmd.ClearMaxDiscountAmount,
            isActive:               cmd.IsActive);

        await _repo.UpsertAsync(promo, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<PromoCodeDetailDto>.Ok(promo.ToDetailDto());
    }

    private static Result<DiscountValue> BuildDiscount(string discountType, decimal amount)
        => discountType.Trim().ToLowerInvariant() switch
        {
            "percentage" => DiscountValue.Percentage(amount),
            "fixed"      => DiscountValue.Fixed(amount),
            _            => Result<DiscountValue>.Fail(new DomainError("INVALID_DISCOUNT_TYPE",
                                $"Unknown discount type '{discountType}'"))
        };
}
```

### `ECommerce.Promotions.Application/Commands/DeactivatePromoCodeCommand.cs`
```csharp
using ECommerce.SharedKernel.CQRS;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Interfaces;
using MediatR;

namespace ECommerce.Promotions.Application.Commands;

public record DeactivatePromoCodeCommand(Guid Id) : IRequest<Result>, ITransactionalCommand;

public class DeactivatePromoCodeCommandHandler : IRequestHandler<DeactivatePromoCodeCommand, Result>
{
    private readonly IPromoCodeRepository _repo;
    private readonly IUnitOfWork          _uow;

    public DeactivatePromoCodeCommandHandler(IPromoCodeRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow  = uow;
    }

    public async Task<Result> Handle(DeactivatePromoCodeCommand cmd, CancellationToken cancellationToken)
    {
        var promo = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (promo is null) return Result.Fail(PromotionsErrors.PromoNotFound);

        promo.Deactivate();
        await _repo.UpsertAsync(promo, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
```

### `ECommerce.Promotions.Application/Commands/DeletePromoCodeCommand.cs`
```csharp
using ECommerce.SharedKernel.CQRS;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Interfaces;
using MediatR;

namespace ECommerce.Promotions.Application.Commands;

public record DeletePromoCodeCommand(Guid Id) : IRequest<Result>, ITransactionalCommand;

public class DeletePromoCodeCommandHandler : IRequestHandler<DeletePromoCodeCommand, Result>
{
    private readonly IPromoCodeRepository _repo;
    private readonly IUnitOfWork          _uow;

    public DeletePromoCodeCommandHandler(IPromoCodeRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow  = uow;
    }

    public async Task<Result> Handle(DeletePromoCodeCommand cmd, CancellationToken cancellationToken)
    {
        var promo = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (promo is null) return Result.Fail(PromotionsErrors.PromoNotFound);

        await _repo.DeleteAsync(promo, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
```

---

## Task 6: Queries

### `ECommerce.Promotions.Application/Queries/GetPromoCodeByIdQuery.cs`
```csharp
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.Mappings;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Interfaces;
using MediatR;

namespace ECommerce.Promotions.Application.Queries;

public record GetPromoCodeByIdQuery(Guid Id) : IRequest<Result<PromoCodeDetailDto>>;

public class GetPromoCodeByIdQueryHandler : IRequestHandler<GetPromoCodeByIdQuery, Result<PromoCodeDetailDto>>
{
    private readonly IPromoCodeRepository _repo;
    public GetPromoCodeByIdQueryHandler(IPromoCodeRepository repo) => _repo = repo;

    public async Task<Result<PromoCodeDetailDto>> Handle(GetPromoCodeByIdQuery q, CancellationToken ct)
    {
        var promo = await _repo.GetByIdAsync(q.Id, ct);
        return promo is null
            ? Result<PromoCodeDetailDto>.Fail(PromotionsErrors.PromoNotFound)
            : Result<PromoCodeDetailDto>.Ok(promo.ToDetailDto());
    }
}
```

### `ECommerce.Promotions.Application/Queries/GetPromoCodesQuery.cs`
```csharp
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.Mappings;
using ECommerce.Promotions.Domain.Interfaces;
using MediatR;

namespace ECommerce.Promotions.Application.Queries;

public record GetPromoCodesQuery(int Page, int PageSize, string? Search, bool? IsActive)
    : IRequest<Result<PaginatedResult<PromoCodeDto>>>;

public class GetPromoCodesQueryHandler : IRequestHandler<GetPromoCodesQuery, Result<PaginatedResult<PromoCodeDto>>>
{
    private readonly IPromoCodeRepository _repo;
    public GetPromoCodesQueryHandler(IPromoCodeRepository repo) => _repo = repo;

    public async Task<Result<PaginatedResult<PromoCodeDto>>> Handle(GetPromoCodesQuery q, CancellationToken ct)
    {
        var (items, total) = await _repo.GetAllAsync(q.Page, q.PageSize, q.Search, q.IsActive, ct);
        return Result<PaginatedResult<PromoCodeDto>>.Ok(new PaginatedResult<PromoCodeDto>
        {
            Items      = items.Select(p => p.ToDto()).ToList(),
            TotalCount = total,
            Page       = q.Page,
            PageSize   = q.PageSize
        });
    }
}
```

### `ECommerce.Promotions.Application/Queries/GetActivePromoCodesQuery.cs`
```csharp
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.Mappings;
using ECommerce.Promotions.Domain.Interfaces;
using MediatR;

namespace ECommerce.Promotions.Application.Queries;

public record GetActivePromoCodesQuery(int Page, int PageSize) : IRequest<Result<PaginatedResult<PromoCodeDto>>>;

public class GetActivePromoCodesQueryHandler : IRequestHandler<GetActivePromoCodesQuery, Result<PaginatedResult<PromoCodeDto>>>
{
    private readonly IPromoCodeRepository _repo;
    public GetActivePromoCodesQueryHandler(IPromoCodeRepository repo) => _repo = repo;

    public async Task<Result<PaginatedResult<PromoCodeDto>>> Handle(GetActivePromoCodesQuery q, CancellationToken ct)
    {
        var (items, total) = await _repo.GetActiveAsync(q.Page, q.PageSize, ct);
        return Result<PaginatedResult<PromoCodeDto>>.Ok(new PaginatedResult<PromoCodeDto>
        {
            Items      = items.Select(p => p.ToDto()).ToList(),
            TotalCount = total,
            Page       = q.Page,
            PageSize   = q.PageSize
        });
    }
}
```

### `ECommerce.Promotions.Application/Queries/ValidatePromoCodeQuery.cs`
```csharp
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.Mappings;
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.Promotions.Domain.Services;
using ECommerce.Promotions.Domain.ValueObjects;
using MediatR;

namespace ECommerce.Promotions.Application.Queries;

/// <summary>
/// Validates a promo code for a given order amount.
///
/// IMPORTANT: This query NEVER returns a failed Result.
/// If the code is invalid, not found, or the order is below the minimum,
/// it returns Result.Ok(new ValidatePromoCodeDto { IsValid = false, ... }).
/// This preserves the existing API contract: POST /validate always returns HTTP 200.
/// </summary>
public record ValidatePromoCodeQuery(string Code, decimal OrderAmount)
    : IRequest<Result<ValidatePromoCodeDto>>;

public class ValidatePromoCodeQueryHandler : IRequestHandler<ValidatePromoCodeQuery, Result<ValidatePromoCodeDto>>
{
    private readonly IPromoCodeRepository _repo;
    private readonly DiscountCalculator   _calculator;

    public ValidatePromoCodeQueryHandler(IPromoCodeRepository repo, DiscountCalculator calculator)
    {
        _repo       = repo;
        _calculator = calculator;
    }

    public async Task<Result<ValidatePromoCodeDto>> Handle(ValidatePromoCodeQuery q, CancellationToken ct)
    {
        var normalizedCode = q.Code.Trim().ToUpperInvariant();
        var promo = await _repo.GetByCodeAsync(normalizedCode, ct);

        if (promo is null)
        {
            return Result<ValidatePromoCodeDto>.Ok(new ValidatePromoCodeDto
            {
                IsValid        = false,
                Message        = "Promo code not found",
                DiscountAmount = 0
            });
        }

        var calcResult = _calculator.Calculate(promo, q.OrderAmount, DateTime.UtcNow);

        if (!calcResult.IsSuccess)
        {
            var message = calcResult.Error!.Code switch
            {
                "PROMO_NOT_VALID"  => "This promo code is not valid",
                "PROMO_MIN_ORDER"  => $"Order amount must meet the minimum required for this code",
                _                  => calcResult.Error.Message
            };
            return Result<ValidatePromoCodeDto>.Ok(new ValidatePromoCodeDto
            {
                IsValid        = false,
                Message        = message,
                DiscountAmount = 0
            });
        }

        return Result<ValidatePromoCodeDto>.Ok(new ValidatePromoCodeDto
        {
            IsValid        = true,
            Message        = "Promo code applied successfully",
            DiscountAmount = calcResult.Value!.DiscountAmount,
            PromoCode      = promo.ToDto()
        });
    }
}
```

---

## Task 7: Verify

```bash
cd src/backend
dotnet build ECommerce.Promotions.Application/ECommerce.Promotions.Application.csproj
```

---

## Acceptance Criteria

- [ ] Project builds with zero errors
- [ ] `ValidatePromoCodeQuery` handler never returns a failed Result — invalid codes return `IsValid=false` inside a successful result
- [ ] `CreatePromoCodeCommand` builds all value objects before touching the repository
- [ ] `UpdatePromoCodeCommand` partial-updates: only provided fields change; unset fields retain current values
- [ ] `DeletePromoCodeCommand` uses hard delete (repo.DeleteAsync)
- [ ] `DeactivatePromoCodeCommand` calls `promo.Deactivate()` (soft-delete via domain method)
- [ ] `GetPromoCodesQuery` and `GetActivePromoCodesQuery` return `PaginatedResult<PromoCodeDto>`
- [ ] No EF, no HTTP, no AutoMapper references in this project
