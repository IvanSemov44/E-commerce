namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when a promo code is not found.
/// </summary>
public sealed class PromoCodeNotFoundException : NotFoundException
{
    public PromoCodeNotFoundException(string code)
        : base($"Promo code '{code}' was not found.")
    {
    }

    public PromoCodeNotFoundException(Guid promoCodeId)
        : base($"Promo code with ID '{promoCodeId}' was not found.")
    {
    }
}
