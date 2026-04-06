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
        var existingProjection = await orderingDbContext.ProductImages
            .FirstOrDefaultAsync(x => x.Id == notification.ImageId, cancellationToken);

        if (notification.IsDeleted)
        {
            if (existingProjection is not null)
            {
                orderingDbContext.ProductImages.Remove(existingProjection);
                await orderingDbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        if (existingProjection is null)
        {
            existingProjection = new ProductImageReadModel
            {
                Id = notification.ImageId
            };
            orderingDbContext.ProductImages.Add(existingProjection);
        }

        existingProjection.ProductId = notification.ProductId;
        existingProjection.Url = notification.Url;
        existingProjection.IsPrimary = notification.IsPrimary;
        existingProjection.UpdatedAt = notification.OccurredAt;

        await orderingDbContext.SaveChangesAsync(cancellationToken);
    }
}
