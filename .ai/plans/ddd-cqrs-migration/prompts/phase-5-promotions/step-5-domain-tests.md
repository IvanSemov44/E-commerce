# Phase 5, Step 5: Domain Unit Tests

**Prerequisite**: Step 1 (Domain) complete.

Create `ECommerce.Promotions.Tests` and write fast, pure-in-memory tests for all domain objects. No database, no HTTP, no MediatR.

---

## Task 1: Create the project

```bash
cd src/backend
dotnet new mstest -n ECommerce.Promotions.Tests -o ECommerce.Promotions.Tests
dotnet sln ECommerce.sln add ECommerce.Promotions.Tests/ECommerce.Promotions.Tests.csproj
dotnet add ECommerce.Promotions.Tests/ECommerce.Promotions.Tests.csproj reference ECommerce.Promotions.Domain/ECommerce.Promotions.Domain.csproj
dotnet add ECommerce.Promotions.Tests/ECommerce.Promotions.Tests.csproj reference ECommerce.Promotions.Application/ECommerce.Promotions.Application.csproj
rm ECommerce.Promotions.Tests/UnitTest1.cs
```

---

## Task 2: PromoCodeStringTests

`ECommerce.Promotions.Tests/Domain/PromoCodeStringTests.cs`

```csharp
using ECommerce.Promotions.Domain.ValueObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Promotions.Tests.Domain;

[TestClass]
public class PromoCodeStringTests
{
    [TestMethod]
    public void Create_ValidCode_ReturnsSuccess()
    {
        var result = PromoCodeString.Create("SAVE20");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("SAVE20", result.Value!.Value);
    }

    [TestMethod]
    public void Create_LowercaseInput_NormalizesToUpper()
    {
        var result = PromoCodeString.Create("save20");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("SAVE20", result.Value!.Value);
    }

    [TestMethod]
    public void Create_WithWhitespace_TrimsAndNormalizes()
    {
        var result = PromoCodeString.Create("  summer10  ");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("SUMMER10", result.Value!.Value);
    }

    [TestMethod]
    public void Create_Empty_ReturnsCodeEmpty()
    {
        var result = PromoCodeString.Create("");
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("CODE_EMPTY", result.Error!.Code);
    }

    [TestMethod]
    public void Create_Whitespace_ReturnsCodeEmpty()
    {
        var result = PromoCodeString.Create("   ");
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("CODE_EMPTY", result.Error!.Code);
    }

    [TestMethod]
    public void Create_TooShort_ReturnsCodeLength()
    {
        var result = PromoCodeString.Create("AB"); // 2 chars < 3
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("CODE_LENGTH", result.Error!.Code);
    }

    [TestMethod]
    public void Create_ExactlyThreeChars_ReturnsSuccess()
    {
        var result = PromoCodeString.Create("ABC");
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void Create_ExactlyTwentyChars_ReturnsSuccess()
    {
        var result = PromoCodeString.Create("ABCDEFGHIJ1234567890"); // 20 chars
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void Create_TwentyOneChars_ReturnsCodeLength()
    {
        var result = PromoCodeString.Create("ABCDEFGHIJ12345678901"); // 21 chars
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("CODE_LENGTH", result.Error!.Code);
    }

    [TestMethod]
    public void Create_InvalidChars_ReturnsCodeChars()
    {
        var result = PromoCodeString.Create("SAVE 20"); // space is not allowed
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("CODE_CHARS", result.Error!.Code);
    }

    [TestMethod]
    public void Create_WithHyphen_ReturnsSuccess()
    {
        var result = PromoCodeString.Create("SUMMER-20");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("SUMMER-20", result.Value!.Value);
    }
}
```

---

## Task 3: DiscountValueTests

`ECommerce.Promotions.Tests/Domain/DiscountValueTests.cs`

```csharp
using ECommerce.Promotions.Domain.ValueObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Promotions.Tests.Domain;

[TestClass]
public class DiscountValueTests
{
    // ── Factory methods ──────────────────────────────

    [TestMethod]
    public void Percentage_Valid_ReturnsSuccess()
    {
        var r = DiscountValue.Percentage(20);
        Assert.IsTrue(r.IsSuccess);
        Assert.AreEqual(20m, r.Value!.Amount);
    }

    [TestMethod]
    public void Percentage_Zero_ReturnsError()
    {
        var r = DiscountValue.Percentage(0);
        Assert.IsFalse(r.IsSuccess);
        Assert.AreEqual("DISCOUNT_PERCENT_RANGE", r.Error!.Code);
    }

    [TestMethod]
    public void Percentage_100_IsValid_Boundary()
    {
        var r = DiscountValue.Percentage(100);
        Assert.IsTrue(r.IsSuccess);
    }

    [TestMethod]
    public void Percentage_Over100_ReturnsError()
    {
        var r = DiscountValue.Percentage(101);
        Assert.IsFalse(r.IsSuccess);
        Assert.AreEqual("DISCOUNT_PERCENT_RANGE", r.Error!.Code);
    }

    [TestMethod]
    public void Fixed_Valid_ReturnsSuccess()
    {
        var r = DiscountValue.Fixed(15);
        Assert.IsTrue(r.IsSuccess);
        Assert.AreEqual(15m, r.Value!.Amount);
    }

    [TestMethod]
    public void Fixed_Zero_ReturnsError()
    {
        var r = DiscountValue.Fixed(0);
        Assert.IsFalse(r.IsSuccess);
        Assert.AreEqual("DISCOUNT_AMOUNT_POSITIVE", r.Error!.Code);
    }

    [TestMethod]
    public void Fixed_Negative_ReturnsError()
    {
        var r = DiscountValue.Fixed(-5);
        Assert.IsFalse(r.IsSuccess);
        Assert.AreEqual("DISCOUNT_AMOUNT_POSITIVE", r.Error!.Code);
    }

    // ── Calculate ────────────────────────────────────

    [TestMethod]
    public void Calculate_Percentage20_Of100_Returns20()
    {
        var discount = DiscountValue.Percentage(20).Value!;
        Assert.AreEqual(20m, discount.Calculate(100m));
    }

    [TestMethod]
    public void Calculate_Percentage_RoundsToTwoDecimals()
    {
        var discount = DiscountValue.Percentage(33).Value!;
        // 33% of 100 = 33.00 — but 33% of 99.99 = 32.9967 → 33.00
        var result = discount.Calculate(99.99m);
        Assert.AreEqual(Math.Round(99.99m * 33 / 100, 2), result);
    }

    [TestMethod]
    public void Calculate_Fixed15_Of100_Returns15()
    {
        var discount = DiscountValue.Fixed(15).Value!;
        Assert.AreEqual(15m, discount.Calculate(100m));
    }

    [TestMethod]
    public void Calculate_Fixed_ExceedsSubtotal_CappedAtSubtotal()
    {
        // Fixed discount of 200 on an order of 100 → can't discount more than 100
        var discount = DiscountValue.Fixed(200).Value!;
        Assert.AreEqual(100m, discount.Calculate(100m));
    }

    [TestMethod]
    public void Calculate_Fixed_EqualToSubtotal_Returns100Percent()
    {
        var discount = DiscountValue.Fixed(50).Value!;
        Assert.AreEqual(50m, discount.Calculate(50m));
    }
}
```

---

## Task 4: DateRangeTests

`ECommerce.Promotions.Tests/Domain/DateRangeTests.cs`

```csharp
using ECommerce.Promotions.Domain.ValueObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Promotions.Tests.Domain;

[TestClass]
public class DateRangeTests
{
    private static readonly DateTime Start = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End   = new(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);

    [TestMethod]
    public void Create_ValidRange_ReturnsSuccess()
    {
        var r = DateRange.Create(Start, End);
        Assert.IsTrue(r.IsSuccess);
        Assert.AreEqual(Start, r.Value!.Start);
        Assert.AreEqual(End,   r.Value.End);
    }

    [TestMethod]
    public void Create_StartEqualsEnd_ReturnsError()
    {
        var r = DateRange.Create(Start, Start);
        Assert.IsFalse(r.IsSuccess);
        Assert.AreEqual("DATE_RANGE_INVALID", r.Error!.Code);
    }

    [TestMethod]
    public void Create_StartAfterEnd_ReturnsError()
    {
        var r = DateRange.Create(End, Start);
        Assert.IsFalse(r.IsSuccess);
        Assert.AreEqual("DATE_RANGE_INVALID", r.Error!.Code);
    }

    [TestMethod]
    public void IsActive_DateWithinRange_ReturnsTrue()
    {
        var range = DateRange.Create(Start, End).Value!;
        Assert.IsTrue(range.IsActive(new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc)));
    }

    [TestMethod]
    public void IsActive_DateBeforeStart_ReturnsFalse()
    {
        var range = DateRange.Create(Start, End).Value!;
        Assert.IsFalse(range.IsActive(new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc)));
    }

    [TestMethod]
    public void IsActive_DateAfterEnd_ReturnsFalse()
    {
        var range = DateRange.Create(Start, End).Value!;
        Assert.IsFalse(range.IsActive(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
    }

    [TestMethod]
    public void IsActive_ExactlyAtStart_ReturnsTrue()
    {
        var range = DateRange.Create(Start, End).Value!;
        Assert.IsTrue(range.IsActive(Start));
    }

    [TestMethod]
    public void IsActive_ExactlyAtEnd_ReturnsTrue()
    {
        var range = DateRange.Create(Start, End).Value!;
        Assert.IsTrue(range.IsActive(End));
    }
}
```

---

## Task 5: PromoCodeTests

`ECommerce.Promotions.Tests/Domain/PromoCodeTests.cs`

```csharp
using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.Events;
using ECommerce.Promotions.Domain.ValueObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Promotions.Tests.Domain;

[TestClass]
public class PromoCodeTests
{
    private static PromoCode BuildCode(
        bool isActive = true,
        int? maxUses = null,
        int usedCount = 0,
        DateRange? validPeriod = null,
        decimal? minOrderAmount = null,
        decimal? maxDiscountAmount = null)
    {
        var code     = PromoCodeString.Create("SAVE20").Value!;
        var discount = DiscountValue.Percentage(20).Value!;
        var promo    = PromoCode.Create(code, discount, validPeriod, maxUses, minOrderAmount, maxDiscountAmount);

        // Simulate stored usedCount via reflection (domain doesn't expose a public setter)
        if (usedCount > 0)
        {
            typeof(PromoCode)
                .GetProperty("UsedCount")!
                .SetValue(promo, usedCount);
        }

        if (!isActive)
        {
            promo.Deactivate();
            promo.ClearDomainEvents(); // clear Deactivate side-effects if needed
        }

        return promo;
    }

    // ── Create ───────────────────────────────────────

    [TestMethod]
    public void Create_SetsSensibleDefaults()
    {
        var promo = BuildCode();
        Assert.IsTrue(promo.IsActive);
        Assert.AreEqual(0, promo.UsedCount);
        Assert.AreNotEqual(Guid.Empty, promo.Id);
    }

    // ── IsValidNow ───────────────────────────────────

    [TestMethod]
    public void IsValidNow_ActiveNoRestrictions_ReturnsTrue()
    {
        var promo = BuildCode();
        Assert.IsTrue(promo.IsValidNow(DateTime.UtcNow));
    }

    [TestMethod]
    public void IsValidNow_Inactive_ReturnsFalse()
    {
        var promo = BuildCode(isActive: false);
        Assert.IsFalse(promo.IsValidNow(DateTime.UtcNow));
    }

    [TestMethod]
    public void IsValidNow_ExpiredValidPeriod_ReturnsFalse()
    {
        var pastRange = DateRange.Create(
            new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2020, 12, 31, 0, 0, 0, DateTimeKind.Utc)).Value!;
        var promo = BuildCode(validPeriod: pastRange);
        Assert.IsFalse(promo.IsValidNow(DateTime.UtcNow));
    }

    [TestMethod]
    public void IsValidNow_FutureValidPeriod_ReturnsFalse()
    {
        var futureRange = DateRange.Create(
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(30)).Value!;
        var promo = BuildCode(validPeriod: futureRange);
        Assert.IsFalse(promo.IsValidNow(DateTime.UtcNow));
    }

    [TestMethod]
    public void IsValidNow_NullValidPeriod_ReturnsTrue()
    {
        // No date restriction = always valid date-wise
        var promo = BuildCode(validPeriod: null);
        Assert.IsTrue(promo.IsValidNow(DateTime.UtcNow));
    }

    [TestMethod]
    public void IsValidNow_MaxUsesReached_ReturnsFalse()
    {
        var promo = BuildCode(maxUses: 5, usedCount: 5);
        Assert.IsFalse(promo.IsValidNow(DateTime.UtcNow));
    }

    [TestMethod]
    public void IsValidNow_NullMaxUses_NoLimit_ReturnsTrue()
    {
        var promo = BuildCode(maxUses: null, usedCount: 999);
        Assert.IsTrue(promo.IsValidNow(DateTime.UtcNow));
    }

    [TestMethod]
    public void IsValidNow_OneUseBelowLimit_ReturnsTrue()
    {
        var promo = BuildCode(maxUses: 5, usedCount: 4);
        Assert.IsTrue(promo.IsValidNow(DateTime.UtcNow));
    }

    // ── RecordUsage ──────────────────────────────────

    [TestMethod]
    public void RecordUsage_ValidCode_IncrementsUsedCount()
    {
        var promo = BuildCode();
        promo.RecordUsage();
        Assert.AreEqual(1, promo.UsedCount);
    }

    [TestMethod]
    public void RecordUsage_ValidCode_RaisesPromoCodeAppliedEvent()
    {
        var promo = BuildCode();
        promo.RecordUsage();
        Assert.IsTrue(promo.DomainEvents.OfType<PromoCodeAppliedEvent>().Any());
    }

    [TestMethod]
    public void RecordUsage_ReachesMaxUses_SetsIsActiveFalse()
    {
        var promo = BuildCode(maxUses: 3, usedCount: 2);
        promo.RecordUsage();
        Assert.IsFalse(promo.IsActive);
    }

    [TestMethod]
    public void RecordUsage_ReachesMaxUses_RaisesPromoCodeExhaustedEvent_NotApplied()
    {
        var promo = BuildCode(maxUses: 3, usedCount: 2);
        promo.RecordUsage();
        Assert.IsTrue(promo.DomainEvents.OfType<PromoCodeExhaustedEvent>().Any());
        Assert.IsFalse(promo.DomainEvents.OfType<PromoCodeAppliedEvent>().Any());
    }

    [TestMethod]
    public void RecordUsage_InvalidCode_ReturnsPromoNotValidError()
    {
        var promo = BuildCode(isActive: false);
        var result = promo.RecordUsage();
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("PROMO_NOT_VALID", result.Error!.Code);
    }

    [TestMethod]
    public void RecordUsage_MaxUsesAlreadyReached_ReturnsError()
    {
        var promo = BuildCode(maxUses: 2, usedCount: 2);
        var result = promo.RecordUsage();
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("PROMO_NOT_VALID", result.Error!.Code);
    }

    // ── Deactivate ───────────────────────────────────

    [TestMethod]
    public void Deactivate_ActiveCode_SetsIsActiveFalse()
    {
        var promo = BuildCode(isActive: true);
        promo.Deactivate();
        Assert.IsFalse(promo.IsActive);
    }

    [TestMethod]
    public void Deactivate_AlreadyInactive_IsIdempotent()
    {
        var promo = BuildCode(isActive: false);
        promo.Deactivate(); // calling again should not throw
        Assert.IsFalse(promo.IsActive);
    }
}
```

---

## Task 6: DiscountCalculatorTests

`ECommerce.Promotions.Tests/Domain/DiscountCalculatorTests.cs`

```csharp
using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.Services;
using ECommerce.Promotions.Domain.ValueObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Promotions.Tests.Domain;

[TestClass]
public class DiscountCalculatorTests
{
    private readonly DiscountCalculator _calculator = new();

    private static PromoCode PercentageCode(decimal percent, decimal? minOrder = null, decimal? maxDiscount = null)
    {
        var code     = PromoCodeString.Create("TEST").Value!;
        var discount = DiscountValue.Percentage(percent).Value!;
        return PromoCode.Create(code, discount, null, null, minOrder, maxDiscount);
    }

    private static PromoCode FixedCode(decimal amount, decimal? minOrder = null, decimal? maxDiscount = null)
    {
        var code     = PromoCodeString.Create("TEST").Value!;
        var discount = DiscountValue.Fixed(amount).Value!;
        return PromoCode.Create(code, discount, null, null, minOrder, maxDiscount);
    }

    // ── Happy paths ───────────────────────────────────

    [TestMethod]
    public void Calculate_Percentage20_On100_Returns20DiscountAnd80Final()
    {
        var promo  = PercentageCode(20);
        var result = _calculator.Calculate(promo, 100m, DateTime.UtcNow);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(20m, result.Value!.DiscountAmount);
        Assert.AreEqual(80m, result.Value.FinalAmount);
    }

    [TestMethod]
    public void Calculate_Fixed15_On100_Returns15DiscountAnd85Final()
    {
        var promo  = FixedCode(15);
        var result = _calculator.Calculate(promo, 100m, DateTime.UtcNow);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(15m, result.Value!.DiscountAmount);
        Assert.AreEqual(85m, result.Value.FinalAmount);
    }

    // ── MaxDiscountAmount cap ────────────────────────

    [TestMethod]
    public void Calculate_PercentageWithMaxDiscountCap_CapsAt100()
    {
        // 50% of 1000 = 500, but maxDiscount = 100 → discount = 100, final = 900
        var promo  = PercentageCode(50, maxDiscount: 100);
        var result = _calculator.Calculate(promo, 1000m, DateTime.UtcNow);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(100m, result.Value!.DiscountAmount);
        Assert.AreEqual(900m, result.Value.FinalAmount);
    }

    [TestMethod]
    public void Calculate_DiscountBelowMaxDiscountCap_NotCapped()
    {
        // 20% of 100 = 20, maxDiscount = 50 → discount stays 20
        var promo  = PercentageCode(20, maxDiscount: 50);
        var result = _calculator.Calculate(promo, 100m, DateTime.UtcNow);
        Assert.AreEqual(20m, result.Value!.DiscountAmount);
    }

    // ── MinimumOrderAmount ───────────────────────────

    [TestMethod]
    public void Calculate_BelowMinOrderAmount_ReturnsPromoMinOrderError()
    {
        var promo  = PercentageCode(20, minOrder: 50);
        var result = _calculator.Calculate(promo, 30m, DateTime.UtcNow);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("PROMO_MIN_ORDER", result.Error!.Code);
    }

    [TestMethod]
    public void Calculate_ExactlyAtMinOrderAmount_Succeeds()
    {
        var promo  = PercentageCode(20, minOrder: 50);
        var result = _calculator.Calculate(promo, 50m, DateTime.UtcNow);
        Assert.IsTrue(result.IsSuccess);
    }

    // ── Invalid code ─────────────────────────────────

    [TestMethod]
    public void Calculate_InactiveCode_ReturnsPromoNotValidError()
    {
        var promo = PercentageCode(20);
        promo.Deactivate();
        var result = _calculator.Calculate(promo, 100m, DateTime.UtcNow);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("PROMO_NOT_VALID", result.Error!.Code);
    }

    [TestMethod]
    public void Calculate_ExpiredCode_ReturnsPromoNotValidError()
    {
        var pastRange = DateRange.Create(
            new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2020, 12, 31, 0, 0, 0, DateTimeKind.Utc)).Value!;
        var code     = PromoCodeString.Create("TEST").Value!;
        var discount = DiscountValue.Percentage(20).Value!;
        var promo    = PromoCode.Create(code, discount, pastRange);
        var result   = _calculator.Calculate(promo, 100m, DateTime.UtcNow);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("PROMO_NOT_VALID", result.Error!.Code);
    }

    [TestMethod]
    public void Calculate_NullValidPeriod_TreatedAsAlwaysValid()
    {
        // No date restriction → valid
        var promo  = PercentageCode(10);
        var result = _calculator.Calculate(promo, 100m, DateTime.UtcNow);
        Assert.IsTrue(result.IsSuccess);
    }
}
```

---

## Task 7: Run

```bash
cd src/backend
dotnet test ECommerce.Promotions.Tests --filter "FullyQualifiedName~Domain" --logger "console;verbosity=normal"
```

---

## Acceptance Criteria

- [ ] All domain tests pass
- [ ] `PromoCodeString.Create` tests cover: empty, too-short, too-long, invalid chars, hyphen, lowercase normalization
- [ ] `DiscountValue.Calculate` tests cover: percentage rounding, fixed capped at subtotal
- [ ] `DateRange` boundary tests: exact-at-start and exact-at-end are inclusive
- [ ] `PromoCode.RecordUsage` raises correct event type depending on whether MaxUses is reached
- [ ] `DiscountCalculator.Calculate` applies MaxDiscountAmount cap
- [ ] `DiscountCalculator.Calculate` returns `PROMO_MIN_ORDER` when subtotal below minimum
- [ ] `DiscountCalculator.Calculate` returns `PROMO_NOT_VALID` for inactive/expired codes
- [ ] Null `ValidPeriod` → always valid (no date restriction)
