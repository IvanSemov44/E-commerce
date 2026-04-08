using ECommerce.Contracts;
using ECommerce.Reviews.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Reviews.Infrastructure.IntegrationEvents;

public sealed class ProductProjectionUpdatedIntegrationEventHandler(ReviewsDbContext reviewsDbContext)
    : INotificationHandler<ProductProjectionUpdatedIntegrationEvent>
{
    public async Task Handle(ProductProjectionUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var existingProjection = await reviewsDbContext.Products
            .FirstOrDefaultAsync(x => x.Id == notification.ProductId, cancellationToken);

        if (notification.IsDeleted)
        {
            if (existingProjection is not null)
            {
                reviewsDbContext.Products.Remove(existingProjection);
                await reviewsDbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        if (existingProjection is null)
        {
            existingProjection = new ProductReadModel
            {
                Id = notification.ProductId
            };
            reviewsDbContext.Products.Add(existingProjection);
        }

        existingProjection.IsActive = true;
        existingProjection.UpdatedAt = notification.OccurredAt == default ? DateTime.UtcNow : notification.OccurredAt;

        await reviewsDbContext.SaveChangesAsync(cancellationToken);
    }
}
