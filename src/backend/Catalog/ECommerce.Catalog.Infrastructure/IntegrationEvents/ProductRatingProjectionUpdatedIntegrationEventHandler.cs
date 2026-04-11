using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Infrastructure.IntegrationEvents;

public sealed class ProductRatingProjectionUpdatedIntegrationEventHandler(CatalogDbContext catalogDbContext)
    : INotificationHandler<ProductRatingProjectionUpdatedIntegrationEvent>
{
    public async Task Handle(ProductRatingProjectionUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var existingProjection = await catalogDbContext.ProductRatings
            .FirstOrDefaultAsync(x => x.ProductId == notification.ProductId, cancellationToken);

        if (existingProjection is null)
        {
            existingProjection = new ProductRatingReadModel
            {
                ProductId = notification.ProductId
            };

            await catalogDbContext.ProductRatings.AddAsync(existingProjection, cancellationToken);
        }

        existingProjection.AverageRating = notification.AverageRating;
        existingProjection.ReviewCount = notification.ReviewCount;
        existingProjection.UpdatedAt = notification.OccurredAt == default ? DateTime.UtcNow : notification.OccurredAt;

        await catalogDbContext.SaveChangesAsync(cancellationToken);
    }
}
