namespace ECommerce.Contracts;

/// <summary>
/// Published by Ordering when an order reaches Delivered status.
/// </summary>
public record OrderDeliveredIntegrationEvent(
    Guid OrderId,
    Guid UserId,
    Guid[] ProductIds,
    DateTime OccurredAt = default)
    : IntegrationEvent
{
    public OrderDeliveredIntegrationEvent()
        : this(Guid.Empty, Guid.Empty, Array.Empty<Guid>())
    {
    }
}
