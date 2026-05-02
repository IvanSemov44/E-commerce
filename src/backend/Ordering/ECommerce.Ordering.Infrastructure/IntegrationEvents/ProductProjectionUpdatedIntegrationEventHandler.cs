using ECommerce.Contracts;
using ECommerce.Ordering.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Infrastructure.IntegrationEvents;

public sealed class ProductProjectionUpdatedIntegrationEventHandler(OrderingDbContext orderingDbContext)
    : INotificationHandler<ProductProjectionUpdatedIntegrationEvent>
{
    public async Task Handle(ProductProjectionUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var alreadyProcessed = await orderingDbContext.InboxMessages
            .AnyAsync(m => m.IdempotencyKey == notification.IdempotencyKey, cancellationToken);
        if (alreadyProcessed)
            return;

        var existingProjection = await orderingDbContext.Products
            .FirstOrDefaultAsync(x => x.Id == notification.ProductId, cancellationToken);

        if (notification.IsDeleted)
        {
            if (existingProjection is not null)
                orderingDbContext.Products.Remove(existingProjection);
        }
        else
        {
            if (existingProjection is null)
            {
                existingProjection = new ProductReadModel { Id = notification.ProductId };
                orderingDbContext.Products.Add(existingProjection);
            }

            existingProjection.Name = notification.Name;
            existingProjection.Price = notification.Price;
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
