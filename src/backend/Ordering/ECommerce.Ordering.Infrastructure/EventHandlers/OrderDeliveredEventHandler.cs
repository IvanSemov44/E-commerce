using ECommerce.Contracts;
using ECommerce.Ordering.Domain.Events;
using ECommerce.Ordering.Infrastructure.Integration;
using MediatR;

namespace ECommerce.Ordering.Infrastructure.EventHandlers;

public sealed class OrderDeliveredEventHandler(IOrderingOutboxEventWriter outboxWriter)
    : INotificationHandler<OrderDeliveredEvent>
{
    public Task Handle(OrderDeliveredEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new OrderDeliveredIntegrationEvent(
            notification.OrderId,
            notification.UserId,
            notification.ProductIds.ToArray(),
            DateTime.UtcNow)
        {
            CorrelationId = notification.OrderId
        };

        return outboxWriter.EnqueueAsync(integrationEvent, cancellationToken);
    }
}
