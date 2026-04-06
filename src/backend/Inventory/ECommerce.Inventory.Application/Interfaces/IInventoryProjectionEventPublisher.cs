namespace ECommerce.Inventory.Application.Interfaces;

public interface IInventoryProjectionEventPublisher
{
    Task PublishStockProjectionUpdatedAsync(
        Guid productId,
        int quantity,
        string reason,
        CancellationToken cancellationToken = default);
}
