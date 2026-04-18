using ECommerce.Shopping.Application.Interfaces;
using ECommerce.Shopping.Domain.Events;
using MediatR;

namespace ECommerce.Shopping.Infrastructure.EventHandlers;

public sealed class CartItemQuantityUpdatedEventHandler(ICartIntegrationEventPublisher publisher)
    : INotificationHandler<CartItemQuantityUpdatedEvent>
{
    public Task Handle(CartItemQuantityUpdatedEvent notification, CancellationToken cancellationToken)
        => publisher.PublishCartItemQuantityUpdatedAsync(
            notification.CartId,
            notification.ProductId,
            notification.NewQuantity,
            cancellationToken);
}
