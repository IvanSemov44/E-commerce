using ECommerce.Contracts;
using ECommerce.Ordering.Application.Interfaces;

namespace ECommerce.Ordering.Infrastructure.Services;

public sealed class OrderIntegrationEventPublisher(IIntegrationEventOutbox outbox) : IOrderIntegrationEventPublisher
{
    public Task PublishOrderPlacedAsync(
        Guid orderId,
        Guid customerId,
        IReadOnlyCollection<Guid> productIds,
        decimal totalAmount,
        CancellationToken cancellationToken = default)
    {
        var integrationEvent = new OrderPlacedIntegrationEvent(
            orderId,
            customerId,
            productIds.ToArray(),
            totalAmount,
            DateTime.UtcNow)
        {
            CorrelationId = orderId
        };

        return outbox.EnqueueAsync(integrationEvent, cancellationToken);
    }
}
