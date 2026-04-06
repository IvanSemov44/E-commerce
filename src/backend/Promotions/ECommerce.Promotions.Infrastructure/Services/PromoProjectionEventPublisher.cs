using ECommerce.Contracts;
using ECommerce.Promotions.Application.Interfaces;
using MediatR;

namespace ECommerce.Promotions.Infrastructure.Services;

public class PromoProjectionEventPublisher(IPublisher publisher) : IPromoProjectionEventPublisher
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

        return publisher.Publish(integrationEvent, cancellationToken);
    }
}
