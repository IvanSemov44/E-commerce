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
        var existingProjection = await orderingDbContext.Products
            .FirstOrDefaultAsync(x => x.Id == notification.ProductId, cancellationToken);

        if (notification.IsDeleted)
        {
            if (existingProjection is not null)
            {
                orderingDbContext.Products.Remove(existingProjection);
                await orderingDbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        if (existingProjection is null)
        {
            existingProjection = new ProductReadModel
            {
                Id = notification.ProductId
            };
            orderingDbContext.Products.Add(existingProjection);
        }

        existingProjection.Name = notification.Name;
        existingProjection.Price = notification.Price;
        existingProjection.UpdatedAt = notification.OccurredAt;

        await orderingDbContext.SaveChangesAsync(cancellationToken);
    }
}
