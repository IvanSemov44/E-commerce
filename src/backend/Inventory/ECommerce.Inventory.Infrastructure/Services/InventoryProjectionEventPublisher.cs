using ECommerce.Contracts;
using ECommerce.Inventory.Application.Interfaces;
using ECommerce.Inventory.Infrastructure.Integration;

namespace ECommerce.Inventory.Infrastructure.Services;

public class InventoryProjectionEventPublisher(IInventoryOutboxEventWriter outbox) : IInventoryProjectionEventPublisher
{
    public Task PublishStockProjectionUpdatedAsync(
        Guid productId,
        int quantity,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var integrationEvent = new InventoryStockProjectionUpdatedIntegrationEvent(
            productId,
            quantity,
            reason,
            DateTime.UtcNow);

        return outbox.EnqueueAsync(integrationEvent, cancellationToken);
    }
}
