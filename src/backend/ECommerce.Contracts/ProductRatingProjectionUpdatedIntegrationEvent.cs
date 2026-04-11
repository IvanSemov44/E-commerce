namespace ECommerce.Contracts;

/// <summary>
/// Published when product rating aggregates change and downstream read models should sync.
/// </summary>
public record ProductRatingProjectionUpdatedIntegrationEvent(
    Guid ProductId,
    decimal AverageRating,
    int ReviewCount,
    DateTime OccurredAt = default)
    : IntegrationEvent
{
    public ProductRatingProjectionUpdatedIntegrationEvent()
        : this(Guid.Empty, 0m, 0)
    {
    }
}
