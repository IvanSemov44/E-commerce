# Phase 5: Promotions Bounded Context

**Prerequisite**: Phase 4 complete.

**Learn**: Domain Services for cross-aggregate logic, the Specification pattern for complex validation, how to pass external data INTO the aggregate instead of injecting services.

---

## What's New in This Phase

Phases 1–4 placed all business logic inside aggregates. This works when logic only needs data from ONE aggregate. The `DiscountCalculator` challenge:

- It needs to know the promo code rules (in `PromoCode` aggregate)
- It needs to know the order subtotal (external value — from the order being placed)
- It cannot live in `PromoCode` because that would require knowing about orders
- It cannot live in a handler because it's business logic, not orchestration

This is when you use a **Domain Service**: business logic that spans multiple aggregates or requires external data, but is still domain logic (not infrastructure).

**Resolving the earlier gap**: The concern was that `DiscountCalculator` in Phase 5 depends on "order subtotal" — but Order doesn't exist until Phase 7. The resolution: `DiscountCalculator` takes a `decimal subtotal` parameter, not an `Order` object. The handler in Phase 7 will pass `order.Subtotal.Amount` to it. No dependency on the Ordering context.

---

## Old Service → New Handler Mapping

| Old Method | New Handler |
|-----------|-------------|
| `PromoCodeService.GetPromoCodeAsync(code)` | `GetPromoCodeQuery` |
| `PromoCodeService.GetAllPromoCodesAsync()` | `GetPromoCodesQuery` (admin) |
| `PromoCodeService.CreatePromoCodeAsync(dto)` | `CreatePromoCodeCommand` |
| `PromoCodeService.UpdatePromoCodeAsync(id, dto)` | `UpdatePromoCodeCommand` |
| `PromoCodeService.DeactivateAsync(id)` | `DeactivatePromoCodeCommand` |
| `PromoCodeService.ValidateCodeAsync(code, subtotal)` | `ValidatePromoCodeQuery` |
| *(called internally by OrderService)* | Used by `PlaceOrderCommand` handler in Phase 7 |

---

## Step 1: Domain Project

### Value Objects

```csharp
// ValueObjects/PromoCodeString.cs
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

        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[A-Z0-9\-]+$"))
            return Result<PromoCodeString>.Fail(PromotionsErrors.CodeChars);

        return Result<PromoCodeString>.Ok(new PromoCodeString(normalized));
    }
}

// ValueObjects/DiscountValue.cs — multi-property: type + amount
public class DiscountValue : ValueObject
{
    public DiscountType Type { get; }   // Percentage or FixedAmount
    public decimal Amount { get; }

    private DiscountValue() { }
    private DiscountValue(DiscountType type, decimal amount) { Type = type; Amount = amount; }

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

    // Domain behavior: compute the actual discount amount for a given subtotal
    public decimal Calculate(decimal subtotal)
    {
        return Type switch
        {
            DiscountType.Percentage => Math.Round(subtotal * Amount / 100, 2),
            DiscountType.Fixed => Math.Min(Amount, subtotal),  // can't discount more than subtotal
            _ => throw new InvalidOperationException($"Unknown discount type: {Type}")
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Amount;
    }
}

// ValueObjects/DateRange.cs — multi-property with partial equality
public class DateRange : ValueObject
{
    public DateTime Start { get; }
    public DateTime End { get; }

    private DateRange() { }
    private DateRange(DateTime start, DateTime end) { Start = start; End = end; }

    public static Result<DateRange> Create(DateTime start, DateTime end)
    {
        if (start >= end)
            return Result<DateRange>.Fail(PromotionsErrors.DateRangeInvalid);
        return Result<DateRange>.Ok(new DateRange(start, end));
    }

    public bool Contains(DateTime date) => date >= Start && date <= End;
    public bool IsActive(DateTime now) => Contains(now);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
```

### PromoCode aggregate

```csharp
// Aggregates/PromoCode/PromoCode.cs
public class PromoCode : AggregateRoot
{
    public PromoCodeString Code { get; private set; } = null!;
    public DiscountValue Discount { get; private set; } = null!;
    public DateRange ValidPeriod { get; private set; } = null!;
    public int? MaxUses { get; private set; }
    public int UsedCount { get; private set; }
    public bool IsActive { get; private set; }
    public decimal? MinimumOrderAmount { get; private set; }

    private PromoCode() { }

    public static PromoCode Create(
        PromoCodeString code,
        DiscountValue discount,
        DateRange validPeriod,
        int? maxUses = null,
        decimal? minimumOrderAmount = null)
    {
        return new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = code,
            Discount = discount,
            ValidPeriod = validPeriod,
            MaxUses = maxUses,
            UsedCount = 0,
            IsActive = true,
            MinimumOrderAmount = minimumOrderAmount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // Domain method: is this code usable right now?
    public bool IsValidNow(DateTime now) =>
        IsActive && ValidPeriod.IsActive(now) && (MaxUses is null || UsedCount < MaxUses);

    // Domain method: record a use (called when an order is placed)
    public Result RecordUsage()
    {
        if (!IsValidNow(DateTime.UtcNow))
            return Result.Fail(PromotionsErrors.PromoNotValid);

        UsedCount++;

        if (MaxUses.HasValue && UsedCount >= MaxUses)
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

    public void Deactivate()
    {
        IsActive = false;
    }
}
```

### Domain Service: DiscountCalculator

```csharp
// Services/DiscountCalculator.cs
// This is a Domain Service: business logic that doesn't fit in a single aggregate
// because it needs the promo code rules AND the order subtotal.
public class DiscountCalculator
{
    // Takes the aggregate and external data as parameters — no injection
    public Result<DiscountCalculation> Calculate(PromoCode promoCode, decimal subtotal, DateTime now)
    {
        if (!promoCode.IsValidNow(now))
            return Result<DiscountCalculation>.Fail(PromotionsErrors.PromoNotValid);

        if (promoCode.MinimumOrderAmount.HasValue && subtotal < promoCode.MinimumOrderAmount.Value)
            return Result<DiscountCalculation>.Fail(PromotionsErrors.PromoMinOrder);

        decimal discountAmount = promoCode.Discount.Calculate(subtotal);

        return Result<DiscountCalculation>.Ok(new DiscountCalculation(
            PromoCodeId: promoCode.Id,
            Code: promoCode.Code.Value,
            DiscountAmount: discountAmount,
            FinalAmount: subtotal - discountAmount));
    }
}

public record DiscountCalculation(
    Guid PromoCodeId,
    string Code,
    decimal DiscountAmount,
    decimal FinalAmount
);
```

**Why a Domain Service and not a static method?** It COULD be a static method here. Domain Services are the right choice when:
- The logic needs to be injected (testable in isolation)
- The logic needs infrastructure data (not the case here)
- The logic will grow in complexity (likely here — future: user-specific discount rules)

For this simple version, a static method on the aggregate would also be defensible. The Domain Service makes it easier to test and extend.

**How the handler uses it in Phase 7:**
```csharp
// In PlaceOrderCommandHandler (Phase 7):
var promoCode = await _promoCodes.GetByCodeAsync(command.PromoCode, ct);
var calculation = _discountCalculator.Calculate(promoCode, command.Subtotal, DateTime.UtcNow);
promoCode.RecordUsage();  // mutates the aggregate, raises event
// ... use calculation.FinalAmount to set order total
```

---

## The Specification Pattern (Optional Enhancement)

The `IsValidNow()` method is simple enough. But if validity rules grow complex (user-specific codes, product-category restrictions, first-purchase-only), the Specification pattern keeps each rule isolated:

```csharp
// Specifications/PromoCodeSpecification.cs
public abstract class PromoCodeSpecification
{
    public abstract bool IsSatisfiedBy(PromoCode code, PromoCodeContext context);

    public PromoCodeSpecification And(PromoCodeSpecification other) =>
        new AndSpecification(this, other);
}

public record PromoCodeContext(decimal Subtotal, Guid? UserId, DateTime Now);

public class ActiveSpecification : PromoCodeSpecification
{
    public override bool IsSatisfiedBy(PromoCode code, PromoCodeContext ctx) =>
        code.IsActive && code.ValidPeriod.IsActive(ctx.Now);
}

public class UsageLimitSpecification : PromoCodeSpecification
{
    public override bool IsSatisfiedBy(PromoCode code, PromoCodeContext ctx) =>
        code.MaxUses is null || code.UsedCount < code.MaxUses;
}

public class MinimumOrderSpecification : PromoCodeSpecification
{
    public override bool IsSatisfiedBy(PromoCode code, PromoCodeContext ctx) =>
        code.MinimumOrderAmount is null || ctx.Subtotal >= code.MinimumOrderAmount;
}
```

**Don't implement this unless it's needed.** The current `IsValidNow()` method is sufficient. Add Specification when you have 3+ independent validation rules that need to be combined, tested independently, or reused separately.

**Tester handoff after Step 1:** Once the `PromoCode` aggregate, value objects, `DiscountCalculator` domain service, and handlers are delivered, the tester writes domain unit tests and handler unit tests in `ECommerce.Promotions.Tests/`. See `.ai/plans/ddd-cqrs-migration/testing/tester-prompt-template.md` → Prompt 2 (domain) and Prompt 3 (handlers).

---

## Definition of Done

Full testing guide: `.ai/plans/ddd-cqrs-migration/testing/README.md`

**Characterization (integration — slow):**
- [ ] Characterization tests written and PASSING against OLD service (before any migration)
- [ ] Characterization tests still PASSING after cutover to new handlers

**Domain unit tests (fast — written after Step 1):**
- [ ] `ECommerce.Promotions.Tests/Domain/PromoCodeTests.cs` written and PASSING
- Covers: RecordUsage limits, Deactivate, IsValidNow, DiscountValue.Calculate, DateRange.IsActive

**Handler unit tests (fast — written after handlers are delivered):**
- [ ] `ECommerce.Promotions.Tests/Handlers/` tests written and PASSING
- Covers: ApplyPromoCodeCommand, DiscountCalculator integration in handler

**Code:**
- [ ] `PromoCode` aggregate with `RecordUsage`, `Deactivate`, `IsValidNow`
- [ ] `DiscountValue` VO with `Calculate(subtotal)` behavior
- [ ] `DateRange` VO with `IsActive(now)` behavior
- [ ] `DiscountCalculator` Domain Service
- [ ] `DiscountCalculation` result record
- [ ] Domain events: `PromoCodeAppliedEvent`, `PromoCodeExhaustedEvent`
- [ ] Old `PromoCodeService` deleted after tests pass

## What You Learned in Phase 5

- Domain Services exist for logic that involves multiple aggregates or external data but is still business logic
- Domain Services take aggregates and external data as parameters — they don't inject services
- Value objects can have behavior (`DiscountValue.Calculate()`, `DateRange.IsActive()`)
- The Specification pattern is an optional tool for complex, composable validation rules — don't use it preemptively
- Resolving cross-phase dependencies: `DiscountCalculator` takes `decimal subtotal`, not `Order` — it doesn't need to know about the Ordering context
