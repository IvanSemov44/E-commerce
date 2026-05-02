using ECommerce.Contracts;
using ECommerce.Payments.Domain.Events;
using ECommerce.Payments.Infrastructure.Integration;
using MediatR;

namespace ECommerce.Payments.Infrastructure.EventHandlers;

public sealed class PaymentFailedEventHandler(IPaymentsOutboxEventWriter outboxWriter)
    : INotificationHandler<PaymentFailedEvent>
{
    public Task Handle(PaymentFailedEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new PaymentFailedIntegrationEvent(
            notification.PaymentId,
            notification.OrderId,
            notification.Reason,
            DateTime.UtcNow)
        {
            CorrelationId = notification.OrderId
        };

        return outboxWriter.EnqueueAsync(integrationEvent, cancellationToken);
    }
}
