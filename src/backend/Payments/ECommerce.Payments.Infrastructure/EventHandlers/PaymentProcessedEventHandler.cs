using ECommerce.Contracts;
using ECommerce.Payments.Domain.Events;
using ECommerce.Payments.Infrastructure.Integration;
using MediatR;

namespace ECommerce.Payments.Infrastructure.EventHandlers;

public sealed class PaymentProcessedEventHandler(IPaymentsOutboxEventWriter outboxWriter)
    : INotificationHandler<PaymentProcessedEvent>
{
    public Task Handle(PaymentProcessedEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new PaymentProcessedIntegrationEvent(
            notification.PaymentId,
            notification.OrderId,
            notification.Amount,
            notification.PaymentMethod,
            DateTime.UtcNow)
        {
            CorrelationId = notification.OrderId
        };

        return outboxWriter.EnqueueAsync(integrationEvent, cancellationToken);
    }
}
