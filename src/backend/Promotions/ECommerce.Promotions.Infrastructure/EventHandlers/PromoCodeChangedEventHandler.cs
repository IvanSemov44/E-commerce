using ECommerce.Contracts;
using ECommerce.Promotions.Domain.Events;
using MediatR;

namespace ECommerce.Promotions.Infrastructure.EventHandlers;

public sealed class PromoCodeChangedEventHandler(IIntegrationEventOutbox outbox)
    : INotificationHandler<PromoCodeChangedEvent>
{
    public Task Handle(PromoCodeChangedEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new PromoCodeProjectionUpdatedIntegrationEvent(
            notification.PromoCodeId,
            notification.Code,
            notification.DiscountValue,
            notification.IsActive,
            notification.IsDeleted,
            notification.OccurredAt);

        return outbox.EnqueueAsync(integrationEvent, cancellationToken);
    }
}
