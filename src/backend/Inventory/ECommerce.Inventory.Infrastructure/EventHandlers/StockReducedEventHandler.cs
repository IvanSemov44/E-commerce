using ECommerce.Inventory.Application.Interfaces;
using ECommerce.Inventory.Domain.Events;
using MediatR;

namespace ECommerce.Inventory.Infrastructure.EventHandlers;

public sealed class StockReducedEventHandler(IInventoryProjectionEventPublisher projectionPublisher)
    : INotificationHandler<StockReducedEvent>
{
    public Task Handle(StockReducedEvent notification, CancellationToken cancellationToken)
        => projectionPublisher.PublishStockProjectionUpdatedAsync(
            notification.ProductId,
            notification.NewQuantity,
            notification.Reason,
            cancellationToken);
}
