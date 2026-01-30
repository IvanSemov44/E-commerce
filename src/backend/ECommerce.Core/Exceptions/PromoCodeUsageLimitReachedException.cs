namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when a promo code has reached its usage limit.
/// </summary>
public sealed class PromoCodeUsageLimitReachedException : BadRequestException
{
    public PromoCodeUsageLimitReachedException()
        : base("Promo code usage limit reached") { }

    public PromoCodeUsageLimitReachedException(string code)
        : base($"Promo code '{code}' has reached its usage limit") { }
}
