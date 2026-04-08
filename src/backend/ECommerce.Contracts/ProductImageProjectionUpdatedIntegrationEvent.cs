namespace ECommerce.Contracts;

/// <summary>
/// Published when product-image projection data changes and downstream read models should sync.
/// </summary>
public record ProductImageProjectionUpdatedIntegrationEvent(
    Guid ImageId,
    Guid ProductId,
    string Url,
    bool IsPrimary,
    bool IsDeleted,
    DateTime OccurredAt = default)
    : IntegrationEvent
{
    public ProductImageProjectionUpdatedIntegrationEvent()
        : this(Guid.Empty, Guid.Empty, string.Empty, false, false)
    {
    }
}
