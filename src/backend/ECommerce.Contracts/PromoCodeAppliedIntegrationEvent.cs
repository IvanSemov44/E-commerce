namespace ECommerce.Contracts;

/// <summary>
/// Published when a promo code is applied during checkout.
/// </summary>
public record PromoCodeAppliedIntegrationEvent(
    Guid PromoCodeId,
    string Code,
    decimal DiscountAmount,
    DateTime OccurredAt = default)
    : IntegrationEvent
{
    public PromoCodeAppliedIntegrationEvent()
        : this(Guid.Empty, string.Empty, 0m)
    {
    }
}
