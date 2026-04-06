using ECommerce.Contracts;
using ECommerce.Promotions.Application.Interfaces;

namespace ECommerce.Promotions.Infrastructure.Services;

public class PromoProjectionEventPublisher(IIntegrationEventOutbox outbox) : IPromoProjectionEventPublisher
{
    public Task PublishPromoProjectionUpdatedAsync(
        Guid promoCodeId,
        string code,
        decimal discountValue,
        bool isActive,
        bool isDeleted,
        CancellationToken cancellationToken = default)
    {
        var integrationEvent = new PromoCodeProjectionUpdatedIntegrationEvent(
            promoCodeId,
            code,
            discountValue,
            isActive,
            isDeleted,
            DateTime.UtcNow);

        return outbox.EnqueueAsync(integrationEvent, cancellationToken);
    }
}
