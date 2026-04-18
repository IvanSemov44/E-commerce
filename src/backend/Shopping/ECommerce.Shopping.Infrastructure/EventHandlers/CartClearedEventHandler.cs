using ECommerce.Shopping.Application.Interfaces;
using ECommerce.Shopping.Domain.Events;
using MediatR;

namespace ECommerce.Shopping.Infrastructure.EventHandlers;

public sealed class CartClearedEventHandler(ICartIntegrationEventPublisher publisher)
    : INotificationHandler<CartClearedEvent>
{
    public Task Handle(CartClearedEvent notification, CancellationToken cancellationToken)
        => publisher.PublishCartClearedAsync(
            notification.CartId,
            notification.UserId,
            cancellationToken);
}
