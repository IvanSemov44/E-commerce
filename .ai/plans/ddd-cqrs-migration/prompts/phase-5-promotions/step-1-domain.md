# Phase 5, Step 1: Domain Project

**Prerequisite**: Step 0 characterization tests passing.

Create `ECommerce.Promotions.Domain` — a class library with no EF, no HTTP, no MediatR.

**What you learn here**: Value objects with behavior (`DiscountValue.Calculate()`), multi-property value objects, nullable value objects on an aggregate, and Domain Services for logic that needs data from multiple sources.

---

## Task 1: Create the project

```bash
cd src/backend
dotnet new classlib -n ECommerce.Promotions.Domain -o ECommerce.Promotions.Domain
dotnet sln ECommerce.sln add ECommerce.Promotions.Domain/ECommerce.Promotions.Domain.csproj
dotnet add ECommerce.Promotions.Domain/ECommerce.Promotions.Domain.csproj reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
rm ECommerce.Promotions.Domain/Class1.cs
```

---

## Task 2: AssemblyInfo

`ECommerce.Promotions.Domain/AssemblyInfo.cs`

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ECommerce.Promotions.Infrastructure")]
[assembly: InternalsVisibleTo("ECommerce.Promotions.Tests")]
```

`InternalsVisibleTo` is needed because `PromoCode`'s private constructor must be `internal` for EF to instantiate it via Infrastructure.

---

## Task 3: Errors

`ECommerce.Promotions.Domain/Errors/PromotionsErrors.cs`

```csharp
using ECommerce.SharedKernel.Results;

namespace ECommerce.Promotions.Domain.Errors;

public static class PromotionsErrors
{
    // Value object validation
    public static readonly DomainError CodeEmpty          = new("CODE_EMPTY", "Promo code cannot be empty");
    public static readonly DomainError CodeLength         = new("CODE_LENGTH", "Promo code must be between 3 and 20 characters");
    public static readonly DomainError CodeChars          = new("CODE_CHARS", "Promo code may only contain letters, digits, and hyphens");
    public static readonly DomainError DiscountPercentRange = new("DISCOUNT_PERCENT_RANGE", "Percentage discount must be between 1 and 100");
    public static readonly DomainError DiscountAmountPositive = new("DISCOUNT_AMOUNT_POSITIVE", "Fixed discount amount must be greater than 0");
    public static readonly DomainError DateRangeInvalid   = new("DATE_RANGE_INVALID", "Start date must be before end date");

    // Business rules — these strings match the OLD error codes so characterization tests don't break
    public static readonly DomainError PromoNotFound      = new("PROMO_CODE_NOT_FOUND", "Promo code not found");
    public static readonly DomainError DuplicateCode      = new("DUPLICATE_PROMO_CODE", "A promo code with this value already exists");
    public static readonly DomainError ConcurrencyConflict = new("CONCURRENCY_CONFLICT", "Promo code was modified by another operation. Please retry.");
    public static readonly DomainError PromoNotValid      = new("PROMO_NOT_VALID", "This promo code is not valid");
    public static readonly DomainError PromoMinOrder      = new("PROMO_MIN_ORDER", "Order amount does not meet the minimum required for this code");
}
```

---

## Task 4: Enums

`ECommerce.Promotions.Domain/Enums/DiscountType.cs`

```csharp
namespace ECommerce.Promotions.Domain.Enums;

public enum DiscountType
{
    Percentage = 0,
    Fixed      = 1
}
```

This must match the existing DB enum values. The old `ECommerce.Core.Enums.DiscountType` uses the same names — confirm with:
```bash
grep -r "DiscountType" src/backend/ECommerce.Core/Enums/ --include="*.cs"
```

---

## Task 5: Value Objects

### `ECommerce.Promotions.Domain/ValueObjects/PromoCodeString.cs`

```csharp
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Errors;
using System.Text.RegularExpressions;

namespace ECommerce.Promotions.Domain.ValueObjects;

public record PromoCodeString
{
    public string Value { get; }

    private PromoCodeString(string value) => Value = value;

    public static Result<PromoCodeString> Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result<PromoCodeString>.Fail(PromotionsErrors.CodeEmpty);

        var normalized = raw.Trim().ToUpperInvariant();

        if (normalized.Length < 3 || normalized.Length > 20)
            return Result<PromoCodeString>.Fail(PromotionsErrors.CodeLength);

        if (!Regex.IsMatch(normalized, @"^[A-Z0-9\-]+$"))
            return Result<PromoCodeString>.Fail(PromotionsErrors.CodeChars);

        return Result<PromoCodeString>.Ok(new PromoCodeString(normalized));
    }

    /// <summary>
    /// For EF reconstitution only. Bypasses validation — assumes the stored value is already valid.
    /// </summary>
    internal static PromoCodeString Reconstitute(string stored) => new(stored);
}
```

### `ECommerce.Promotions.Domain/ValueObjects/DiscountValue.cs`

```csharp
using ECommerce.SharedKernel;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Enums;
using ECommerce.Promotions.Domain.Errors;

namespace ECommerce.Promotions.Domain.ValueObjects;

/// <summary>
/// Represents a discount: either a percentage or a fixed amount.
/// Has behavior: Calculate(subtotal) returns the raw discount amount.
/// MaxDiscountAmount capping is applied by DiscountCalculator, not here.
/// </summary>
public class DiscountValue : ValueObject
{
    public DiscountType Type   { get; private set; }
    public decimal      Amount { get; private set; }

    private DiscountValue() { }

    private DiscountValue(DiscountType type, decimal amount)
    {
        Type   = type;
        Amount = amount;
    }

    public static Result<DiscountValue> Percentage(decimal percent)
    {
        if (percent <= 0 || percent > 100)
            return Result<DiscountValue>.Fail(PromotionsErrors.DiscountPercentRange);

        return Result<DiscountValue>.Ok(new DiscountValue(DiscountType.Percentage, percent));
    }

    public static Result<DiscountValue> Fixed(decimal amount)
    {
        if (amount <= 0)
            return Result<DiscountValue>.Fail(PromotionsErrors.DiscountAmountPositive);

        return Result<DiscountValue>.Ok(new DiscountValue(DiscountType.Fixed, amount));
    }

    /// <summary>
    /// Calculates the raw discount for a given subtotal.
    /// Fixed discount is capped at subtotal (final amount cannot go below zero).
    /// MaxDiscountAmount cap is NOT applied here — DiscountCalculator handles that.
    /// </summary>
    public decimal Calculate(decimal subtotal)
    {
        return Type switch
        {
            DiscountType.Percentage => Math.Round(subtotal * Amount / 100, 2),
            DiscountType.Fixed      => Math.Min(Amount, subtotal),
            _ => throw new InvalidOperationException($"Unknown discount type: {Type}")
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Amount;
    }
}
```

### `ECommerce.Promotions.Domain/ValueObjects/DateRange.cs`

```csharp
using ECommerce.SharedKernel;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Errors;

namespace ECommerce.Promotions.Domain.ValueObjects;

/// <summary>
/// An inclusive date range. When a PromoCode has no ValidPeriod (null), the code is always valid date-wise.
/// </summary>
public class DateRange : ValueObject
{
    public DateTime Start { get; private set; }
    public DateTime End   { get; private set; }

    private DateRange() { }

    private DateRange(DateTime start, DateTime end)
    {
        Start = start;
        End   = end;
    }

    public static Result<DateRange> Create(DateTime start, DateTime end)
    {
        if (start >= end)
            return Result<DateRange>.Fail(PromotionsErrors.DateRangeInvalid);

        return Result<DateRange>.Ok(new DateRange(start, end));
    }

    public bool IsActive(DateTime now) => now >= Start && now <= End;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
```

---

## Task 6: Domain Events

`ECommerce.Promotions.Domain/Events/PromoCodeAppliedEvent.cs`

```csharp
using ECommerce.SharedKernel;

namespace ECommerce.Promotions.Domain.Events;

/// <summary>Raised when a promo code usage is recorded and the code is NOT yet exhausted.</summary>
public record PromoCodeAppliedEvent(Guid PromoCodeId, string Code) : IDomainEvent;
```

`ECommerce.Promotions.Domain/Events/PromoCodeExhaustedEvent.cs`

```csharp
using ECommerce.SharedKernel;

namespace ECommerce.Promotions.Domain.Events;

/// <summary>Raised when RecordUsage pushes UsedCount to MaxUses, auto-deactivating the code.</summary>
public record PromoCodeExhaustedEvent(Guid PromoCodeId, string Code) : IDomainEvent;
```

---

## Task 7: PromoCode Aggregate

`ECommerce.Promotions.Domain/Aggregates/PromoCode/PromoCode.cs`

```csharp
using ECommerce.SharedKernel;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Events;
using ECommerce.Promotions.Domain.ValueObjects;

namespace ECommerce.Promotions.Domain.Aggregates.PromoCode;

public sealed class PromoCode : AggregateRoot
{
    public PromoCodeString Code              { get; private set; } = null!;
    public DiscountValue   Discount          { get; private set; } = null!;
    public DateRange?      ValidPeriod       { get; private set; }   // null = no date restriction
    public int?            MaxUses           { get; private set; }
    public int             UsedCount         { get; private set; }
    public bool            IsActive          { get; private set; }
    public decimal?        MinimumOrderAmount { get; private set; }
    public decimal?        MaxDiscountAmount  { get; private set; }
    public byte[]?         RowVersion        { get; private set; }

    private PromoCode() { }

    public static PromoCode Create(
        PromoCodeString code,
        DiscountValue discount,
        DateRange? validPeriod,
        int? maxUses = null,
        decimal? minimumOrderAmount = null,
        decimal? maxDiscountAmount = null)
    {
        return new PromoCode
        {
            Id                 = Guid.NewGuid(),
            Code               = code,
            Discount           = discount,
            ValidPeriod        = validPeriod,
            MaxUses            = maxUses,
            UsedCount          = 0,
            IsActive           = true,
            MinimumOrderAmount = minimumOrderAmount,
            MaxDiscountAmount  = maxDiscountAmount,
            CreatedAt          = DateTime.UtcNow,
            UpdatedAt          = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Returns true if this code can be applied right now.
    /// A null ValidPeriod means always valid date-wise.
    /// </summary>
    public bool IsValidNow(DateTime now)
    {
        if (!IsActive) return false;
        if (ValidPeriod is not null && !ValidPeriod.IsActive(now)) return false;
        if (MaxUses.HasValue && UsedCount >= MaxUses.Value) return false;
        return true;
    }

    /// <summary>
    /// Records one use of this code. Call after order is confirmed.
    /// Raises PromoCodeAppliedEvent or PromoCodeExhaustedEvent.
    /// </summary>
    public Result RecordUsage()
    {
        if (!IsValidNow(DateTime.UtcNow))
            return Result.Fail(PromotionsErrors.PromoNotValid);

        UsedCount++;
        UpdatedAt = DateTime.UtcNow;

        if (MaxUses.HasValue && UsedCount >= MaxUses.Value)
        {
            IsActive = false;
            AddDomainEvent(new PromoCodeExhaustedEvent(Id, Code.Value));
        }
        else
        {
            AddDomainEvent(new PromoCodeAppliedEvent(Id, Code.Value));
        }

        return Result.Ok();
    }

    /// <summary>Soft-deletes the code. Idempotent — deactivating an already-inactive code is safe.</summary>
    public void Deactivate()
    {
        IsActive  = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Partial update — only non-null arguments are applied.
    /// </summary>
    public Result Update(
        PromoCodeString? code = null,
        DiscountValue? discount = null,
        DateRange? validPeriod = null,
        bool clearValidPeriod = false,
        int? maxUses = null,
        bool clearMaxUses = false,
        decimal? minimumOrderAmount = null,
        bool clearMinimumOrderAmount = false,
        decimal? maxDiscountAmount = null,
        bool clearMaxDiscountAmount = false,
        bool? isActive = null)
    {
        if (code is not null)     Code    = code;
        if (discount is not null) Discount = discount;

        if (clearValidPeriod)            ValidPeriod = null;
        else if (validPeriod is not null) ValidPeriod = validPeriod;

        if (clearMaxUses)          MaxUses = null;
        else if (maxUses.HasValue) MaxUses = maxUses;

        if (clearMinimumOrderAmount)             MinimumOrderAmount = null;
        else if (minimumOrderAmount.HasValue)    MinimumOrderAmount = minimumOrderAmount;

        if (clearMaxDiscountAmount)              MaxDiscountAmount = null;
        else if (maxDiscountAmount.HasValue)     MaxDiscountAmount = maxDiscountAmount;

        if (isActive.HasValue) IsActive = isActive.Value;

        UpdatedAt = DateTime.UtcNow;
        return Result.Ok();
    }
}
```

---

## Task 8: Repository Interface

`ECommerce.Promotions.Domain/Interfaces/IPromoCodeRepository.cs`

```csharp
using ECommerce.Promotions.Domain.Aggregates.PromoCode;

namespace ECommerce.Promotions.Domain.Interfaces;

public interface IPromoCodeRepository
{
    Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PromoCode?> GetByCodeAsync(string normalizedCode, CancellationToken ct = default);
    Task<(List<PromoCode> Items, int TotalCount)> GetActiveAsync(int page, int pageSize, CancellationToken ct = default);
    Task<(List<PromoCode> Items, int TotalCount)> GetAllAsync(int page, int pageSize, string? search, bool? isActive, CancellationToken ct = default);
    Task UpsertAsync(PromoCode promoCode, CancellationToken ct = default);
    Task DeleteAsync(PromoCode promoCode, CancellationToken ct = default);
}
```

---

## Task 9: Domain Service — DiscountCalculator

`ECommerce.Promotions.Domain/Services/DiscountCalculator.cs`

```csharp
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.Errors;

namespace ECommerce.Promotions.Domain.Services;

/// <summary>
/// Domain Service: calculates a discount for a given order subtotal.
///
/// Why a Domain Service and not a method on PromoCode?
/// Because it needs two pieces of information from different sources:
/// the PromoCode rules AND the order subtotal (an external value).
/// The PromoCode aggregate must not know about orders.
///
/// Usage in Phase 7:
///   var calc = _calculator.Calculate(promoCode, order.Subtotal.Amount, DateTime.UtcNow);
///   promoCode.RecordUsage();
/// </summary>
public class DiscountCalculator
{
    public Result<DiscountCalculation> Calculate(PromoCode promoCode, decimal subtotal, DateTime now)
    {
        if (!promoCode.IsValidNow(now))
            return Result<DiscountCalculation>.Fail(PromotionsErrors.PromoNotValid);

        if (promoCode.MinimumOrderAmount.HasValue && subtotal < promoCode.MinimumOrderAmount.Value)
            return Result<DiscountCalculation>.Fail(PromotionsErrors.PromoMinOrder);

        decimal discountAmount = promoCode.Discount.Calculate(subtotal);

        // Apply MaxDiscountAmount cap if set
        if (promoCode.MaxDiscountAmount.HasValue && discountAmount > promoCode.MaxDiscountAmount.Value)
            discountAmount = promoCode.MaxDiscountAmount.Value;

        return Result<DiscountCalculation>.Ok(new DiscountCalculation(
            PromoCodeId:    promoCode.Id,
            Code:           promoCode.Code.Value,
            DiscountAmount: discountAmount,
            FinalAmount:    subtotal - discountAmount));
    }
}

public record DiscountCalculation(
    Guid    PromoCodeId,
    string  Code,
    decimal DiscountAmount,
    decimal FinalAmount);
```

---

## Task 10: Verify

```bash
cd src/backend
dotnet build ECommerce.Promotions.Domain/ECommerce.Promotions.Domain.csproj
```

Zero errors expected. The rest of the app is unchanged.

---

## Acceptance Criteria

- [ ] Project builds with zero errors
- [ ] `PromoCodeString.Create("save20")` returns `"SAVE20"` (normalized)
- [ ] `PromoCodeString.Create("")` returns `CodeEmpty` error
- [ ] `DiscountValue.Percentage(20).Calculate(100)` returns `20`
- [ ] `DiscountValue.Fixed(15).Calculate(100)` returns `15`
- [ ] `DiscountValue.Fixed(200).Calculate(100)` returns `100` (capped at subtotal)
- [ ] `DateRange` with `start >= end` returns `DateRangeInvalid`
- [ ] `PromoCode.IsValidNow` returns false when IsActive=false
- [ ] `PromoCode.IsValidNow` returns true when ValidPeriod is null (no date restriction)
- [ ] `PromoCode.RecordUsage` raises `PromoCodeAppliedEvent` normally
- [ ] `PromoCode.RecordUsage` raises `PromoCodeExhaustedEvent` and sets IsActive=false when MaxUses reached
- [ ] `DiscountCalculator.Calculate` returns `PromoMinOrder` when subtotal < MinimumOrderAmount
- [ ] `DiscountCalculator.Calculate` applies MaxDiscountAmount cap correctly
- [ ] No EF, no MediatR, no HTTP references in this project
