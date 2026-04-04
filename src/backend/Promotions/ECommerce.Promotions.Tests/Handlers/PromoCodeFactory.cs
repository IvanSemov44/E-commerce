using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.ValueObjects;

namespace ECommerce.Promotions.Tests.Handlers;

public static class PromoCodeFactory
{
    public static PromoCode Active(
        string code = "SAVE20",
        decimal percent = 20,
        DateTime? createdAt = null)
    {
        return PromoCode.Create(
            PromoCodeString.Create(code).GetDataOrThrow(),
            DiscountValue.Percentage(percent).GetDataOrThrow(),
            validPeriod: null,
            createdAt: createdAt);
    }

    public static PromoCode ActiveWithPeriod(
        string code = "SAVE20",
        decimal percent = 20,
        DateTime? validFrom = null,
        DateTime? validUntil = null,
        int? maxUses = null,
        decimal? minimumOrderAmount = null,
        decimal? maxDiscountAmount = null,
        DateTime? createdAt = null)
    {
        DateRange? validPeriod = null;
        if (validFrom.HasValue && validUntil.HasValue)
        {
            validPeriod = DateRange.Create(validFrom.Value, validUntil.Value).GetDataOrThrow();
        }

        return PromoCode.Create(
            PromoCodeString.Create(code).GetDataOrThrow(),
            DiscountValue.Percentage(percent).GetDataOrThrow(),
            validPeriod,
            maxUses,
            minimumOrderAmount,
            maxDiscountAmount,
            createdAt: createdAt);
    }

    public static PromoCode WithMaxUses(string code, int maxUses, int usedCount = 0)
    {
        var promo = Active(code);
        typeof(PromoCode).GetProperty(nameof(PromoCode.MaxUses))!.SetValue(promo, maxUses);
        typeof(PromoCode).GetProperty(nameof(PromoCode.UsedCount))!.SetValue(promo, usedCount);
        return promo;
    }
}
