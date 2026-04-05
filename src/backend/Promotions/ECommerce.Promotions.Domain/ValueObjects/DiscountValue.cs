using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Enums;
using ECommerce.Promotions.Domain.Errors;

namespace ECommerce.Promotions.Domain.ValueObjects;

public class DiscountValue : ValueObject
{
    public DiscountType Type { get; private set; }
    public decimal Amount { get; private set; }

    private DiscountValue() { }

    private DiscountValue(DiscountType type, decimal amount)
    {
        Type = type;
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

    public decimal Calculate(decimal subtotal)
    {
        return Type switch
        {
            DiscountType.Percentage => Math.Round(subtotal * Amount / 100, 2),
            DiscountType.Fixed => Math.Min(Amount, subtotal),
            _ => throw new InvalidOperationException($"Unknown discount type: {Type}")
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Amount;
    }
}