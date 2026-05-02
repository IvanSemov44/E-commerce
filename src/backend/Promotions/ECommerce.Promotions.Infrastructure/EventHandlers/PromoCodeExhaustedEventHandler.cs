using ECommerce.Contracts;
using ECommerce.Promotions.Domain.Events;
using MediatR;

namespace ECommerce.Promotions.Infrastructure.EventHandlers;

/// <summary>
/// When a promo code is exhausted (used up) it's effectively deactivated.
/// Publish a projection-update so read models reflect the new inactive state.
/// </summary>
public sealed class PromoCodeExhaustedEventHandler(IIntegrationEventOutbox outbox)
    : INotificationHandler<PromoCodeExhaustedEvent>
{
    public Task Handle(PromoCodeExhaustedEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new PromoCodeProjectionUpdatedIntegrationEvent(
            notification.PromoCodeId,
            notification.Code,
            0m,
            false,
            false,
            notification.OccurredAt);

        return outbox.EnqueueAsync(integrationEvent, cancellationToken);
    }
}
