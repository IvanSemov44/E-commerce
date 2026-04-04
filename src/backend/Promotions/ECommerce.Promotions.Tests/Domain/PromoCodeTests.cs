using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.Enums;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Events;
using ECommerce.Promotions.Domain.ValueObjects;

namespace ECommerce.Promotions.Tests.Domain;

[TestClass]
public class PromoCodeTests
{
    private static PromoCode CreateValidPromoCode(string code = "SAVE20")
    {
        var codeVo = PromoCodeString.Create(code).GetDataOrThrow();
        var discount = DiscountValue.Percentage(20).GetDataOrThrow();
        var validPeriod = DateRange.Create(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(30)).GetDataOrThrow();

        return PromoCode.Create(codeVo, discount, validPeriod, maxUses: 100);
    }

    [TestMethod]
    public void Create_ValidData_ReturnsActivePromoCode()
    {
        var promoCode = CreateValidPromoCode();

        Assert.IsTrue(promoCode.IsActive);
        Assert.AreEqual(0, promoCode.UsedCount);
        Assert.AreEqual("SAVE20", promoCode.Code.Value);
    }

    [TestMethod]
    public void IsValidNow_ActiveAndInPeriod_ReturnsTrue()
    {
        var promoCode = CreateValidPromoCode();

        Assert.IsTrue(promoCode.IsValidNow(DateTime.UtcNow));
    }

    [TestMethod]
    public void IsValidNow_Inactive_ReturnsFalse()
    {
        var promoCode = CreateValidPromoCode();
        promoCode.Deactivate();

        Assert.IsFalse(promoCode.IsValidNow(DateTime.UtcNow));
    }

    [TestMethod]
    public void IsValidNow_Expired_ReturnsFalse()
    {
        var codeVo = PromoCodeString.Create("EXPIRED").GetDataOrThrow();
        var discount = DiscountValue.Percentage(10).GetDataOrThrow();
        var validPeriod = DateRange.Create(
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(-1)).GetDataOrThrow();

        var promoCode = PromoCode.Create(codeVo, discount, validPeriod);

        Assert.IsFalse(promoCode.IsValidNow(DateTime.UtcNow));
    }

    [TestMethod]
    public void IsValidNow_NotYetStarted_ReturnsFalse()
    {
        var codeVo = PromoCodeString.Create("FUTURE").GetDataOrThrow();
        var discount = DiscountValue.Percentage(10).GetDataOrThrow();
        var validPeriod = DateRange.Create(
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(30)).GetDataOrThrow();

        var promoCode = PromoCode.Create(codeVo, discount, validPeriod);

        Assert.IsFalse(promoCode.IsValidNow(DateTime.UtcNow));
    }

    [TestMethod]
    public void IsValidNow_MaxUsesReached_ReturnsFalse()
    {
        var promoCode = CreateValidPromoCode();
        for (int i = 0; i < 100; i++)
        {
            promoCode.RecordUsage();
        }

        Assert.IsFalse(promoCode.IsValidNow(DateTime.UtcNow));
    }

    [TestMethod]
    public void RecordUsage_WithinLimits_IncrementsUsedCount()
    {
        var promoCode = CreateValidPromoCode();

        var result = promoCode.RecordUsage();

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, promoCode.UsedCount);
    }

    [TestMethod]
    public void RecordUsage_AtMaxUsageLimit_Fails()
    {
        var codeVo = PromoCodeString.Create("ONCE").GetDataOrThrow();
        var discount = DiscountValue.Percentage(10).GetDataOrThrow();
        var validPeriod = DateRange.Create(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(30)).GetDataOrThrow();

        var promoCode = PromoCode.Create(codeVo, discount, validPeriod, maxUses: 1);
        promoCode.RecordUsage();

        var result = promoCode.RecordUsage();

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(PromotionsErrors.PromoNotValid, result.GetErrorOrThrow());
    }

    [TestMethod]
    public void RecordUsage_ExhaustsLastUse_RaisesExhaustedEvent()
    {
        var codeVo = PromoCodeString.Create("LAST").GetDataOrThrow();
        var discount = DiscountValue.Percentage(10).GetDataOrThrow();
        var validPeriod = DateRange.Create(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(30)).GetDataOrThrow();

        var promoCode = PromoCode.Create(codeVo, discount, validPeriod, maxUses: 1);

        promoCode.RecordUsage();

        Assert.AreEqual(1, promoCode.DomainEvents.Count);
        Assert.IsInstanceOfType<PromoCodeExhaustedEvent>(promoCode.DomainEvents.First());
    }

    [TestMethod]
    public void RecordUsage_StillHasUses_RaisesAppliedEvent()
    {
        var promoCode = CreateValidPromoCode();

        promoCode.RecordUsage();

        Assert.HasCount(1, promoCode.DomainEvents);
        Assert.IsInstanceOfType<PromoCodeAppliedEvent>(promoCode.DomainEvents.First());
    }

    [TestMethod]
    public void Deactivate_SetsInactive()
    {
        var promoCode = CreateValidPromoCode();

        promoCode.Deactivate();

        Assert.IsFalse(promoCode.IsActive);
    }

    [TestMethod]
    public void DiscountValue_Percentage_CalculatesCorrectly()
    {
        var discount = DiscountValue.Percentage(20).GetDataOrThrow();

        var result = discount.Calculate(100m);

        Assert.AreEqual(20m, result);
    }

    [TestMethod]
    public void DiscountValue_Fixed_CalculatesCorrectly()
    {
        var discount = DiscountValue.Fixed(15).GetDataOrThrow();

        var result = discount.Calculate(100m);

        Assert.AreEqual(15m, result);
    }

    [TestMethod]
    public void DiscountValue_Fixed_ExceedsSubtotal_CappedAtSubtotal()
    {
        var discount = DiscountValue.Fixed(50).GetDataOrThrow();

        var result = discount.Calculate(30m);

        Assert.AreEqual(30m, result);
    }

    [TestMethod]
    public void DateRange_IsActive_WithinRange_ReturnsTrue()
    {
        var start = DateTime.UtcNow.AddDays(-1);
        var end = DateTime.UtcNow.AddDays(1);
        var range = DateRange.Create(start, end).GetDataOrThrow();

        Assert.IsTrue(range.IsActive(DateTime.UtcNow));
    }

    [TestMethod]
    public void DateRange_IsActive_BeforeStart_ReturnsFalse()
    {
        var start = DateTime.UtcNow.AddDays(1);
        var end = DateTime.UtcNow.AddDays(2);
        var range = DateRange.Create(start, end).GetDataOrThrow();

        Assert.IsFalse(range.IsActive(DateTime.UtcNow));
    }

    [TestMethod]
    public void DateRange_IsActive_AfterEnd_ReturnsFalse()
    {
        var start = DateTime.UtcNow.AddDays(-2);
        var end = DateTime.UtcNow.AddDays(-1);
        var range = DateRange.Create(start, end).GetDataOrThrow();

        Assert.IsFalse(range.IsActive(DateTime.UtcNow));
    }
}
