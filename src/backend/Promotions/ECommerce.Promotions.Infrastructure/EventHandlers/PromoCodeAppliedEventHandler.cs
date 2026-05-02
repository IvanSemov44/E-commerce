using ECommerce.Contracts;
using ECommerce.Promotions.Domain.Events;
using MediatR;

namespace ECommerce.Promotions.Infrastructure.EventHandlers;

public sealed class PromoCodeAppliedEventHandler(IIntegrationEventOutbox outbox)
    : INotificationHandler<PromoCodeAppliedEvent>
{
    public Task Handle(PromoCodeAppliedEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new PromoCodeAppliedIntegrationEvent(
            notification.PromoCodeId,
            notification.Code,
            0m,
            notification.OccurredAt);

        return outbox.EnqueueAsync(integrationEvent, cancellationToken);
    }
}
