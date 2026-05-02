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
        var alreadyProcessed = await orderingDbContext.InboxMessages
            .AnyAsync(m => m.IdempotencyKey == notification.IdempotencyKey, cancellationToken);
        if (alreadyProcessed)
            return;

        var existingProjection = await orderingDbContext.PromoCodes
            .FirstOrDefaultAsync(x => x.Id == notification.PromoCodeId, cancellationToken);

        if (notification.IsDeleted)
        {
            if (existingProjection is not null)
                orderingDbContext.PromoCodes.Remove(existingProjection);
        }
        else
        {
            if (existingProjection is null)
            {
                existingProjection = new PromoCodeReadModel { Id = notification.PromoCodeId };
                orderingDbContext.PromoCodes.Add(existingProjection);
            }

            existingProjection.Code = notification.Code;
            existingProjection.DiscountValue = notification.DiscountValue;
            existingProjection.IsActive = notification.IsActive;
            existingProjection.UpdatedAt = notification.OccurredAt;
        }

        orderingDbContext.InboxMessages.Add(new InboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = notification.IdempotencyKey,
            EventType = notification.GetType().Name,
            ReceivedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            AttemptCount = 1
        });

        await orderingDbContext.SaveChangesAsync(cancellationToken);
    }
}
