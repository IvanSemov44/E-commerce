namespace ECommerce.Contracts;

/// <summary>
/// Published when inventory quantity changes and downstream read models should sync.
/// </summary>
public record InventoryStockProjectionUpdatedIntegrationEvent(
    Guid ProductId,
    int Quantity,
    string Reason,
    DateTime OccurredAt = default)
    : IntegrationEvent
{
    public InventoryStockProjectionUpdatedIntegrationEvent()
        : this(Guid.Empty, 0, string.Empty)
    {
    }
}
