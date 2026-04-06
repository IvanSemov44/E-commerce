namespace ECommerce.Contracts;

/// <summary>
/// Published when product projection data changes and downstream read models should sync.
/// </summary>
public record ProductProjectionUpdatedIntegrationEvent(
    Guid ProductId,
    string Name,
    decimal Price,
    bool IsDeleted,
    DateTime OccurredAt = default)
    : IntegrationEvent
{
    public ProductProjectionUpdatedIntegrationEvent()
        : this(Guid.Empty, string.Empty, 0m, false)
    {
    }
}
