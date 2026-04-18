using ECommerce.Shopping.Application.Interfaces;
using ECommerce.Shopping.Domain.Events;
using MediatR;

namespace ECommerce.Shopping.Infrastructure.EventHandlers;

public sealed class ItemAddedToCartEventHandler(ICartIntegrationEventPublisher publisher)
    : INotificationHandler<ItemAddedToCartEvent>
{
    public Task Handle(ItemAddedToCartEvent notification, CancellationToken cancellationToken)
        => publisher.PublishCartItemAddedAsync(
            notification.CartId,
            notification.ProductId,
            notification.Quantity,
            cancellationToken);
}
