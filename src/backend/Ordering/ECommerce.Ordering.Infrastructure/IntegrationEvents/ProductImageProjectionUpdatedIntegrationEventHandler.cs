using ECommerce.Contracts;
using ECommerce.Ordering.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Infrastructure.IntegrationEvents;

public sealed class ProductImageProjectionUpdatedIntegrationEventHandler(OrderingDbContext orderingDbContext)
    : INotificationHandler<ProductImageProjectionUpdatedIntegrationEvent>
{
    public async Task Handle(ProductImageProjectionUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var alreadyProcessed = await orderingDbContext.InboxMessages
            .AnyAsync(m => m.IdempotencyKey == notification.IdempotencyKey, cancellationToken);
        if (alreadyProcessed)
            return;

        var existingProjection = await orderingDbContext.ProductImages
            .FirstOrDefaultAsync(x => x.Id == notification.ImageId, cancellationToken);

        if (notification.IsDeleted)
        {
            if (existingProjection is not null)
                orderingDbContext.ProductImages.Remove(existingProjection);
        }
        else
        {
            if (existingProjection is null)
            {
                existingProjection = new ProductImageReadModel { Id = notification.ImageId };
                orderingDbContext.ProductImages.Add(existingProjection);
            }

            existingProjection.ProductId = notification.ProductId;
            existingProjection.Url = notification.Url;
            existingProjection.IsPrimary = notification.IsPrimary;
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
