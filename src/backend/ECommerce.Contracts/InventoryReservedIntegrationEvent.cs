namespace ECommerce.Contracts;

/// <summary>
/// Published by Inventory when stock reservation succeeds for an order.
/// </summary>
public record InventoryReservedIntegrationEvent(
    Guid OrderId,
    Guid[] ProductIds,
    int[] Quantities,
    DateTime OccurredAt = default)
    : IntegrationEvent
{
    public InventoryReservedIntegrationEvent()
        : this(Guid.Empty, Array.Empty<Guid>(), Array.Empty<int>())
    {
    }
}
