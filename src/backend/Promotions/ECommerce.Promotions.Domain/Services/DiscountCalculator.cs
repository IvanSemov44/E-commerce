using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.Errors;

namespace ECommerce.Promotions.Domain.Services;

public static class DiscountCalculator
{
    public static Result<DiscountCalculation> Calculate(PromoCode promoCode, decimal subtotal, DateTime now)
    {
        if (!promoCode.IsValidNow(now))
            return Result<DiscountCalculation>.Fail(PromotionsErrors.PromoNotValid);

        if (promoCode.MinimumOrderAmount.HasValue && subtotal < promoCode.MinimumOrderAmount.Value)
            return Result<DiscountCalculation>.Fail(PromotionsErrors.PromoMinOrder);

        decimal discountAmount = promoCode.Discount.Calculate(subtotal);

        if (promoCode.MaxDiscountAmount.HasValue && discountAmount > promoCode.MaxDiscountAmount.Value)
            discountAmount = promoCode.MaxDiscountAmount.Value;

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
    decimal FinalAmount);