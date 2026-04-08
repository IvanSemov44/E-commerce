using ECommerce.Contracts;
using ECommerce.Inventory.Application.Interfaces;

namespace ECommerce.Inventory.Infrastructure.Services;

public class InventoryProjectionEventPublisher(IIntegrationEventOutbox outbox) : IInventoryProjectionEventPublisher
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
