using ECommerce.Contracts;
using ECommerce.Inventory.Application.Interfaces;
using MediatR;

namespace ECommerce.Inventory.Infrastructure.Services;

public class InventoryProjectionEventPublisher(IMediator mediator) : IInventoryProjectionEventPublisher
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

        return mediator.Publish(integrationEvent, cancellationToken);
    }
}
