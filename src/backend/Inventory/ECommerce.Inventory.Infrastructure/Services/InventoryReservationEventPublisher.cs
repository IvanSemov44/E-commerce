using ECommerce.Contracts;
using ECommerce.Inventory.Application.Interfaces;

namespace ECommerce.Inventory.Infrastructure.Services;

public sealed class InventoryReservationEventPublisher(IIntegrationEventOutbox outbox) : IInventoryReservationEventPublisher
{
    public Task PublishInventoryReservedAsync(
        Guid orderId,
        IReadOnlyCollection<Guid> productIds,
        IReadOnlyCollection<int> quantities,
        CancellationToken cancellationToken = default)
    {
        var integrationEvent = new InventoryReservedIntegrationEvent(
            orderId,
            productIds.ToArray(),
            quantities.ToArray(),
            DateTime.UtcNow)
        {
            CorrelationId = orderId
        };

        return outbox.EnqueueAsync(integrationEvent, cancellationToken);
    }

    public Task PublishInventoryReservationFailedAsync(
        Guid orderId,
        Guid productId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var integrationEvent = new InventoryReservationFailedIntegrationEvent(
            orderId,
            productId,
            reason,
            DateTime.UtcNow)
        {
            CorrelationId = orderId
        };

        return outbox.EnqueueAsync(integrationEvent, cancellationToken);
    }
}
