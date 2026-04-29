using ECommerce.Inventory.Application.Interfaces;
using ECommerce.Inventory.Domain.Events;
using MediatR;

namespace ECommerce.Inventory.Infrastructure.EventHandlers;

public sealed class StockAdjustedEventHandler(IInventoryProjectionEventPublisher projectionPublisher)
    : INotificationHandler<StockAdjustedEvent>
{
    public Task Handle(StockAdjustedEvent notification, CancellationToken cancellationToken)
        => projectionPublisher.PublishStockProjectionUpdatedAsync(
            notification.ProductId,
            notification.NewQuantity,
            notification.Reason,
            cancellationToken);
}
