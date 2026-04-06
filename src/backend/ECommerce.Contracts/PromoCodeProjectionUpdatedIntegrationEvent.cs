namespace ECommerce.Contracts;

/// <summary>
/// Published when promo-code projection data changes and downstream read models should sync.
/// </summary>
public record PromoCodeProjectionUpdatedIntegrationEvent(
    Guid PromoCodeId,
    string Code,
    decimal DiscountValue,
    bool IsActive,
    bool IsDeleted,
    DateTime OccurredAt = default)
    : IntegrationEvent
{
    public PromoCodeProjectionUpdatedIntegrationEvent()
        : this(Guid.Empty, string.Empty, 0m, false, false)
    {
    }
}
