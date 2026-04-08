namespace ECommerce.Contracts;

/// <summary>
/// Published by Ordering when a new order is created.
/// </summary>
public record OrderPlacedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    Guid[] ProductIds,
    decimal TotalAmount,
    DateTime OccurredAt = default)
    : IntegrationEvent
{
    public OrderPlacedIntegrationEvent()
        : this(Guid.Empty, Guid.Empty, Array.Empty<Guid>(), 0m)
    {
    }
}
