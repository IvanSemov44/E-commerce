using ECommerce.Contracts;
using ECommerce.Payments.Domain.Events;
using ECommerce.Payments.Infrastructure.Integration;
using MediatR;

namespace ECommerce.Payments.Infrastructure.EventHandlers;

public sealed class PaymentRefundedEventHandler(IPaymentsOutboxEventWriter outboxWriter)
    : INotificationHandler<PaymentRefundedEvent>
{
    public Task Handle(PaymentRefundedEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new PaymentRefundedIntegrationEvent(
            notification.PaymentId,
            notification.OrderId,
            notification.Amount,
            DateTime.UtcNow)
        {
            CorrelationId = notification.OrderId
        };

        return outboxWriter.EnqueueAsync(integrationEvent, cancellationToken);
    }
}
