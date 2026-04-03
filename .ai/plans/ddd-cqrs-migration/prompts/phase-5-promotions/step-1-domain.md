# Phase 5, Step 1: Promotions Domain Project

**Prerequisite**: Phase 4 (Shopping) complete and all tests pass.

---

## Context

This step creates `ECommerce.Promotions.Domain` — the single domain project for the Promotions bounded context. PromoCode is the only aggregate.

**New concepts in this phase:**
- `PromoCodeString` is a value object that normalises the code to UPPER-CASE on create and validates format. It has a separate `Reconstitute` factory for EF Core that bypasses validation (the stored value is already valid).
- `DiscountValue` is a value object that knows how to calculate a raw discount from a subtotal. The MaxDiscountAmount cap is a separate field on the aggregate and is applied by `DiscountCalculator`, not by `DiscountValue`.
- `DateRange` is a nullable value object. A PromoCode with no `ValidPeriod` is always date-valid.
- `DiscountCalculator` is a **domain service** (not injected via interface) — it is a concrete class registered as Scoped in DI and injected into the Application layer handlers that need it.
- `PromoCode.RecordUsage()` exists on the aggregate but is **not wired to an HTTP endpoint in Phase 5**. It will be called by `PlaceOrderCommandHandler` in Phase 7.

---

## Task: Create ECommerce.Promotions.Domain Project

### 1. Create the project

```bash
cd src/backend
mkdir -p Promotions
dotnet new classlib -n ECommerce.Promotions.Domain -f net10.0 -o Promotions/ECommerce.Promotions.Domain
dotnet sln ../../ECommerce.sln add Promotions/ECommerce.Promotions.Domain/ECommerce.Promotions.Domain.csproj

dotnet add Promotions/ECommerce.Promotions.Domain/ECommerce.Promotions.Domain.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj

rm Promotions/ECommerce.Promotions.Domain/Class1.cs
```

### 2. Create AssemblyInfo

**File: `Promotions/ECommerce.Promotions.Domain/Properties/AssemblyInfo.cs`**

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ECommerce.Promotions.Infrastructure")]
```

### 3. Create the DiscountType enum

**File: `Promotions/ECommerce.Promotions.Domain/Enums/DiscountType.cs`**

```csharp
namespace ECommerce.Promotions.Domain.Enums;

/// <summary>
/// Maps to existing DB column values — do not rename without a migration.
/// "Percentage" = percentage off subtotal (0-100%).
/// "Fixed"      = fixed monetary amount off.
/// </summary>
public enum DiscountType
{
    Percentage,
    Fixed
}
```

### 4. Create domain errors

**File: `Promotions/ECommerce.Promotions.Domain/Errors/PromotionsErrors.cs`**

```csharp
using ECommerce.SharedKernel.Results;

namespace ECommerce.Promotions.Domain.Errors;

/// <summary>
/// All domain errors for the Promotions bounded context.
///
/// IMPORTANT: The string values in DomainError.Code must match the values that
/// the characterization tests expect. Do not change existing Code strings.
/// New validation errors (CodeEmpty, etc.) use new codes and are safe to name freely.
/// </summary>
public static class PromotionsErrors
{
    // ── Value-object validation (new codes, safe to name) ─────────────────────
    public static readonly DomainError CodeEmpty            = new("CODE_EMPTY",              "Promo code must not be empty.");
    public static readonly DomainError CodeLength           = new("CODE_LENGTH",             "Promo code must be between 3 and 20 characters.");
    public static readonly DomainError CodeChars            = new("CODE_CHARS",              "Promo code may only contain A-Z, 0-9, and hyphens.");
    public static readonly DomainError DiscountPercentRange = new("DISCOUNT_PERCENT_RANGE",  "Percentage discount must be between 1 and 100.");
    public static readonly DomainError DiscountAmountPositive = new("DISCOUNT_AMOUNT_POSITIVE", "Fixed discount amount must be greater than zero.");
    public static readonly DomainError DateRangeInvalid     = new("DATE_RANGE_INVALID",      "Start date must be before end date.");

    // ── Business errors — codes must NOT change (characterization tests pin them) ──
    public static readonly DomainError PromoNotFound        = new("PROMO_CODE_NOT_FOUND",    "Promo code not found.");
    public static readonly DomainError DuplicateCode        = new("DUPLICATE_PROMO_CODE",    "A promo code with this code already exists.");
    public static readonly DomainError ConcurrencyConflict  = new("CONCURRENCY_CONFLICT",    "The promo code was modified by another request. Please retry.");
    public static readonly DomainError PromoNotValid        = new("PROMO_NOT_VALID",         "This promo code is not currently valid.");
    public static readonly DomainError PromoMinOrder        = new("PROMO_MIN_ORDER",         "Order amount does not meet the minimum required for this promo code.");
}
```

### 5. Create PromoCodeString value object

**File: `Promotions/ECommerce.Promotions.Domain/ValueObjects/PromoCodeString.cs`**

```csharp
using System.Text.RegularExpressions;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Errors;

namespace ECommerce.Promotions.Domain.ValueObjects;

/// <summary>
/// Strongly-typed promo code string.
/// Rules: 3-20 characters, A-Z, 0-9, hyphen only. Stored and compared in UPPER-CASE.
///
/// Two factories:
///   Create(string)      — validates and normalises. Use from application layer and API.
///   Reconstitute(string) — bypasses validation. Use from EF Core only (value is already stored, already valid).
/// </summary>
public sealed record PromoCodeString
{
    private static readonly Regex _validPattern =
        new(@"^[A-Z0-9\-]{3,20}$", RegexOptions.Compiled);

    public string Value { get; }

    private PromoCodeString(string value) => Value = value;

    public static Result<PromoCodeString> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result<PromoCodeString>.Fail(PromotionsErrors.CodeEmpty);

        var upper = raw.Trim().ToUpperInvariant();

        if (upper.Length < 3 || upper.Length > 20)
            return Result<PromoCodeString>.Fail(PromotionsErrors.CodeLength);

        if (!_validPattern.IsMatch(upper))
            return Result<PromoCodeString>.Fail(PromotionsErrors.CodeChars);

        return Result<PromoCodeString>.Ok(new PromoCodeString(upper));
    }

    /// <summary>
    /// EF Core factory — skips validation. The value stored in the database is assumed valid.
    /// Do NOT call from application or domain code.
    /// </summary>
    internal static PromoCodeString Reconstitute(string value) => new(value);

    public override string ToString() => Value;
}
```

### 6. Create DiscountValue value object

**File: `Promotions/ECommerce.Promotions.Domain/ValueObjects/DiscountValue.cs`**

```csharp
using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Enums;
using ECommerce.Promotions.Domain.Errors;

namespace ECommerce.Promotions.Domain.ValueObjects;

/// <summary>
/// Encapsulates a discount type + amount and can calculate the raw discount for a subtotal.
///
/// NOTE: MaxDiscountAmount cap is NOT applied here — it lives on the PromoCode aggregate
/// and is enforced by DiscountCalculator.
/// NOTE: Fixed discount is capped at subtotal (can never produce negative final amount).
/// </summary>
public sealed class DiscountValue : ValueObject
{
    public DiscountType Type   { get; }
    public decimal      Amount { get; }

    private DiscountValue(DiscountType type, decimal amount)
    {
        Type   = type;
        Amount = amount;
    }

    public static Result<DiscountValue> Percentage(decimal percent)
    {
        // Allow 1..100 inclusive; 0 is not a meaningful discount
        if (percent <= 0m || percent > 100m)
            return Result<DiscountValue>.Fail(PromotionsErrors.DiscountPercentRange);

        return Result<DiscountValue>.Ok(new DiscountValue(DiscountType.Percentage, percent));
    }

    public static Result<DiscountValue> Fixed(decimal amount)
    {
        if (amount <= 0m)
            return Result<DiscountValue>.Fail(PromotionsErrors.DiscountAmountPositive);

        return Result<DiscountValue>.Ok(new DiscountValue(DiscountType.Fixed, amount));
    }

    /// <summary>
    /// Creates a DiscountValue from raw DB values (EF Core use).
    /// Bypasses business validation — stored values are assumed valid.
    /// </summary>
    internal static DiscountValue Reconstitute(DiscountType type, decimal amount)
        => new(type, amount);

    /// <summary>
    /// Calculates the raw discount amount for the given subtotal.
    /// Fixed discount is capped at the subtotal (FinalAmount can never be negative).
    /// MaxDiscountAmount cap (if any) is applied by the caller (DiscountCalculator).
    /// </summary>
    public decimal Calculate(decimal subtotal)
    {
        if (subtotal <= 0m) return 0m;

        return Type switch
        {
            DiscountType.Percentage => Math.Round(subtotal * (Amount / 100m), 2, MidpointRounding.AwayFromZero),
            DiscountType.Fixed      => Math.Min(Amount, subtotal), // never produce negative final amount
            _                      => 0m
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Amount;
    }
}
```

### 7. Create DateRange value object

**File: `Promotions/ECommerce.Promotions.Domain/ValueObjects/DateRange.cs`**

```csharp
using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Errors;

namespace ECommerce.Promotions.Domain.ValueObjects;

/// <summary>
/// An inclusive date range (Start..End).
/// A PromoCode with ValidPeriod == null is always date-valid (no expiry).
/// A PromoCode with a ValidPeriod is valid when now is within [Start, End].
/// </summary>
public sealed class DateRange : ValueObject
{
    public DateTime Start { get; }
    public DateTime End   { get; }

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

    /// <summary>Returns true if <paramref name="now"/> falls within [Start, End] inclusive.</summary>
    public bool IsActive(DateTime now) => now >= Start && now <= End;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
```

### 8. Create domain events

**File: `Promotions/ECommerce.Promotions.Domain/Events/PromoCodeAppliedEvent.cs`**

```csharp
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Promotions.Domain.Events;

/// <summary>Raised when RecordUsage() is called on a PromoCode (i.e. when an order applies it).</summary>
public record PromoCodeAppliedEvent(
    Guid   PromoCodeId,
    string Code
) : DomainEventBase;
```

**File: `Promotions/ECommerce.Promotions.Domain/Events/PromoCodeExhaustedEvent.cs`**

```csharp
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Promotions.Domain.Events;

/// <summary>
/// Raised when RecordUsage() pushes UsedCount to MaxUses, causing the code to be auto-deactivated.
/// </summary>
public record PromoCodeExhaustedEvent(
    Guid   PromoCodeId,
    string Code
) : DomainEventBase;
```

### 9. Create the PromoCode aggregate

**File: `Promotions/ECommerce.Promotions.Domain/Aggregates/PromoCode/PromoCode.cs`**

```csharp
using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Events;
using ECommerce.Promotions.Domain.ValueObjects;

namespace ECommerce.Promotions.Domain.Aggregates.PromoCode;

/// <summary>
/// PromoCode aggregate root.
///
/// ValidPeriod == null means the code is not date-restricted (always-valid period).
/// MaxUses == null means no usage limit.
/// RecordUsage() is called by PlaceOrderCommandHandler (Phase 7) — not an HTTP endpoint in Phase 5.
/// </summary>
public sealed class PromoCode : AggregateRoot
{
    public PromoCodeString  Code                 { get; private set; } = null!;
    public DiscountValue    Discount             { get; private set; } = null!;
    public DateRange?       ValidPeriod          { get; private set; }
    public int?             MaxUses              { get; private set; }
    public int              UsedCount            { get; private set; }
    public bool             IsActive             { get; private set; }
    public decimal?         MinimumOrderAmount   { get; private set; }
    public decimal?         MaxDiscountAmount    { get; private set; }
    public byte[]?          RowVersion           { get; private set; } // EF concurrency token

    private PromoCode() { } // EF Core

    /// <summary>
    /// Factory used by CreatePromoCodeCommandHandler.
    /// Returns a failure result if any value-object validation fails.
    /// </summary>
    public static Result<PromoCode> Create(
        string            code,
        DiscountValue     discount,
        DateRange?        validPeriod,
        int?              maxUses,
        bool              isActive,
        decimal?          minimumOrderAmount,
        decimal?          maxDiscountAmount)
    {
        var codeResult = PromoCodeString.Create(code);
        if (!codeResult.IsSuccess)
            return Result<PromoCode>.Fail(codeResult.GetErrorOrThrow());

        return Result<PromoCode>.Ok(new PromoCode
        {
            Id                  = Guid.NewGuid(),
            Code                = codeResult.GetDataOrThrow(),
            Discount            = discount,
            ValidPeriod         = validPeriod,
            MaxUses             = maxUses,
            UsedCount           = 0,
            IsActive            = isActive,
            MinimumOrderAmount  = minimumOrderAmount,
            MaxDiscountAmount   = maxDiscountAmount,
        });
    }

    /// <summary>
    /// Returns true when the code can be applied right now.
    /// Checks: IsActive, date range (if set), usage limit (if set).
    /// </summary>
    public bool IsValidNow(DateTime now)
    {
        if (!IsActive) return false;

        // Null ValidPeriod means always date-valid
        if (ValidPeriod is not null && !ValidPeriod.IsActive(now)) return false;

        // Null MaxUses means unlimited
        if (MaxUses.HasValue && UsedCount >= MaxUses.Value) return false;

        return true;
    }

    /// <summary>
    /// Records one usage of this code.
    /// Raises PromoCodeAppliedEvent always.
    /// If UsedCount reaches MaxUses, deactivates the code and raises PromoCodeExhaustedEvent.
    /// Returns PromoNotValid if the code is not currently valid.
    /// Called by PlaceOrderCommandHandler (Phase 7).
    /// </summary>
    public Result RecordUsage(DateTime now)
    {
        if (!IsValidNow(now))
            return Result.Fail(PromotionsErrors.PromoNotValid);

        UsedCount++;
        AddDomainEvent(new PromoCodeAppliedEvent(Id, Code.Value));

        if (MaxUses.HasValue && UsedCount >= MaxUses.Value)
        {
            IsActive = false;
            AddDomainEvent(new PromoCodeExhaustedEvent(Id, Code.Value));
        }

        return Result.Ok();
    }

    /// <summary>Soft-deactivates this code (IsActive = false).</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Partial update — only non-null arguments replace existing values.
    /// Returns a failure if any new value-object construction fails.
    /// </summary>
    public Result Update(
        string?       newCode,
        DiscountValue? newDiscount,
        DateRange?    newValidPeriod,
        bool?         newValidPeriodPresent, // true = set validPeriod to newValidPeriod (even if null = clear it)
        int?          newMaxUses,
        bool?         newMaxUsesPresent,
        bool?         newIsActive,
        decimal?      newMinimumOrderAmount,
        decimal?      newMaxDiscountAmount)
    {
        if (newCode is not null)
        {
            var codeResult = PromoCodeString.Create(newCode);
            if (!codeResult.IsSuccess) return Result.Fail(codeResult.GetErrorOrThrow());
            Code = codeResult.GetDataOrThrow();
        }

        if (newDiscount is not null)
            Discount = newDiscount;

        if (newValidPeriodPresent == true)
            ValidPeriod = newValidPeriod; // can be set to null (clear expiry)

        if (newMaxUsesPresent == true)
            MaxUses = newMaxUses; // can be set to null (clear limit)

        if (newIsActive.HasValue)
            IsActive = newIsActive.Value;

        if (newMinimumOrderAmount.HasValue)
            MinimumOrderAmount = newMinimumOrderAmount.Value;

        if (newMaxDiscountAmount.HasValue)
            MaxDiscountAmount = newMaxDiscountAmount.Value;

        return Result.Ok();
    }
}
```

> **Design note on `Update`**: The two `*Present` booleans allow the caller to distinguish "not supplied" (null = keep existing) from "explicitly set to null" (null = clear). This preserves the existing partial-update semantics without requiring sentinel values.

### 10. Create the repository interface

**File: `Promotions/ECommerce.Promotions.Domain/Interfaces/IPromoCodeRepository.cs`**

```csharp
using ECommerce.Promotions.Domain.Aggregates.PromoCode;

namespace ECommerce.Promotions.Domain.Interfaces;

public interface IPromoCodeRepository
{
    Task<PromoCode?>  GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PromoCode?>  GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Returns only IsActive=true codes, paginated, ordered by Code.</summary>
    Task<(IReadOnlyList<PromoCode> Items, int TotalCount)> GetActiveAsync(
        int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Admin list: optional search on Code, optional isActive filter, paginated.
    /// </summary>
    Task<(IReadOnlyList<PromoCode> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? search, bool? isActive, CancellationToken ct = default);

    Task UpsertAsync(PromoCode promoCode, CancellationToken ct = default);
    Task DeleteAsync(PromoCode promoCode, CancellationToken ct = default);
}
```

### 11. Create the DiscountCalculator domain service

**File: `Promotions/ECommerce.Promotions.Domain/Services/DiscountCalculator.cs`**

```csharp
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.Errors;

namespace ECommerce.Promotions.Domain.Services;

/// <summary>
/// Domain service that applies a PromoCode to a subtotal and returns the discount.
/// Not injected via interface — register as Scoped and inject the concrete class.
///
/// Responsibilities:
///   1. Check IsValidNow (date, active flag, usage limit).
///   2. Check MinimumOrderAmount.
///   3. Calculate raw discount via DiscountValue.Calculate.
///   4. Apply MaxDiscountAmount cap if set.
///   5. Return a DiscountCalculation record.
/// </summary>
public sealed class DiscountCalculator
{
    public Result<DiscountCalculation> Calculate(
        PromoCode promoCode,
        decimal   subtotal,
        DateTime  now)
    {
        if (!promoCode.IsValidNow(now))
            return Result<DiscountCalculation>.Fail(PromotionsErrors.PromoNotValid);

        if (promoCode.MinimumOrderAmount.HasValue && subtotal < promoCode.MinimumOrderAmount.Value)
            return Result<DiscountCalculation>.Fail(PromotionsErrors.PromoMinOrder);

        // Raw discount (DiscountValue handles its own type logic; Fixed is already capped at subtotal)
        var rawDiscount = promoCode.Discount.Calculate(subtotal);

        // Apply MaxDiscountAmount cap if set
        var discountAmount = promoCode.MaxDiscountAmount.HasValue
            ? Math.Min(rawDiscount, promoCode.MaxDiscountAmount.Value)
            : rawDiscount;

        var finalAmount = Math.Max(0m, subtotal - discountAmount);

        return Result<DiscountCalculation>.Ok(new DiscountCalculation(
            promoCode.Id,
            promoCode.Code.Value,
            discountAmount,
            finalAmount));
    }
}
```

**File: `Promotions/ECommerce.Promotions.Domain/Services/DiscountCalculation.cs`**

```csharp
namespace ECommerce.Promotions.Domain.Services;

/// <summary>Output of DiscountCalculator.Calculate.</summary>
public record DiscountCalculation(
    Guid    PromoCodeId,
    string  Code,
    decimal DiscountAmount,
    decimal FinalAmount
);
```

### 12. Verify

```bash
cd src/backend
dotnet build Promotions/ECommerce.Promotions.Domain/ECommerce.Promotions.Domain.csproj
dotnet build  # Entire solution still builds
```

---

## Tester Handoff

Once this step is delivered, the tester writes domain unit tests in `ECommerce.Promotions.Tests/Domain/`. See `step-5-domain-tests.md`.

---

## Acceptance Criteria

- [ ] `ECommerce.Promotions.Domain` project created and added to solution
- [ ] Only dependency: `ECommerce.SharedKernel`
- [ ] `AssemblyInfo.cs` with `InternalsVisibleTo("ECommerce.Promotions.Infrastructure")`
- [ ] `DiscountType` enum: `Percentage`, `Fixed`
- [ ] `PromotionsErrors`: all error constants present; existing business-error Code strings preserved (PROMO_CODE_NOT_FOUND, DUPLICATE_PROMO_CODE, etc.)
- [ ] `PromoCodeString`: `Create` validates + normalises; `Reconstitute` is `internal` and bypasses validation
- [ ] `DiscountValue`: `Percentage` factory (1-100 inclusive), `Fixed` factory (>0), `Calculate` method; Fixed capped at subtotal; `Reconstitute` is `internal`
- [ ] `DateRange`: `Create` requires start < end; `IsActive(now)` returns bool
- [ ] `PromoCode` aggregate: `Create`, `IsValidNow`, `RecordUsage` (raises events), `Deactivate`, `Update`
- [ ] `PromoCode.ValidPeriod` is `DateRange?` — null = always date-valid
- [ ] `PromoCode.MaxUses` is `int?` — null = no limit
- [ ] `IPromoCodeRepository` with all five methods (GetById, GetByCode, GetActive, GetAll, Upsert, Delete)
- [ ] `DiscountCalculator` service: checks IsValidNow, MinimumOrderAmount, calculates with cap
- [ ] `DiscountCalculation` record: PromoCodeId, Code, DiscountAmount, FinalAmount
- [ ] `PromoCodeAppliedEvent` and `PromoCodeExhaustedEvent`
- [ ] `dotnet build` passes
