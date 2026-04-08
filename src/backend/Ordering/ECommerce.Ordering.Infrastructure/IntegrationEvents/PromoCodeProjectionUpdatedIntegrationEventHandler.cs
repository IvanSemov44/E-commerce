using ECommerce.Contracts;
using ECommerce.Ordering.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Infrastructure.IntegrationEvents;

public sealed class PromoCodeProjectionUpdatedIntegrationEventHandler(OrderingDbContext orderingDbContext)
    : INotificationHandler<PromoCodeProjectionUpdatedIntegrationEvent>
{
    public async Task Handle(PromoCodeProjectionUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var existingProjection = await orderingDbContext.PromoCodes
            .FirstOrDefaultAsync(x => x.Id == notification.PromoCodeId, cancellationToken);

        if (notification.IsDeleted)
        {
            if (existingProjection is not null)
            {
                orderingDbContext.PromoCodes.Remove(existingProjection);
                await orderingDbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        if (existingProjection is null)
        {
            existingProjection = new PromoCodeReadModel
            {
                Id = notification.PromoCodeId
            };
            orderingDbContext.PromoCodes.Add(existingProjection);
        }

        existingProjection.Code = notification.Code;
        existingProjection.DiscountValue = notification.DiscountValue;
        existingProjection.IsActive = notification.IsActive;
        existingProjection.UpdatedAt = notification.OccurredAt;

        await orderingDbContext.SaveChangesAsync(cancellationToken);
    }
}
