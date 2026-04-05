namespace ECommerce.Contracts;

/// <summary>
/// Published by Inventory when stock reservation fails for an order.
/// </summary>
public record InventoryReservationFailedIntegrationEvent(
    Guid OrderId,
    Guid ProductId,
    string Reason,
    DateTime OccurredAt = default)
    : IntegrationEvent
{
    public InventoryReservationFailedIntegrationEvent()
        : this(Guid.Empty, Guid.Empty, string.Empty)
    {
    }
}
