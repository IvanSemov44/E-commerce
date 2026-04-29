using ECommerce.Inventory.Application.Interfaces;
using ECommerce.Inventory.Domain.Events;
using MediatR;

namespace ECommerce.Inventory.Infrastructure.EventHandlers;

public sealed class StockReplenishedEventHandler(IInventoryProjectionEventPublisher projectionPublisher)
    : INotificationHandler<StockReplenishedEvent>
{
    public Task Handle(StockReplenishedEvent notification, CancellationToken cancellationToken)
        => projectionPublisher.PublishStockProjectionUpdatedAsync(
            notification.ProductId,
            notification.NewQuantity,
            "replenished",
            cancellationToken);
}
