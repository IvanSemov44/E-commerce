using ECommerce.Contracts;
using ECommerce.Ordering.Domain.Events;
using ECommerce.Ordering.Infrastructure.Integration;
using MediatR;

namespace ECommerce.Ordering.Infrastructure.EventHandlers;

public sealed class OrderPlacedEventHandler(IOrderingOutboxEventWriter outboxWriter)
    : INotificationHandler<OrderPlacedEvent>
{
    public Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new OrderPlacedIntegrationEvent(
            notification.OrderId,
            notification.UserId,
            notification.Items.Select(i => i.ProductId).ToArray(),
            notification.Total,
            DateTime.UtcNow)
        {
            CorrelationId = notification.OrderId,
            Quantities = notification.Items.Select(i => i.Quantity).ToArray()
        };

        return outboxWriter.EnqueueAsync(integrationEvent, cancellationToken);
    }
}
