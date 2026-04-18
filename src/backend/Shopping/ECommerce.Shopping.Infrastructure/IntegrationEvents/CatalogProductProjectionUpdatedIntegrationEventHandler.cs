using ECommerce.Contracts;
using ECommerce.Shopping.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shopping.Infrastructure.IntegrationEvents;

public sealed class CatalogProductProjectionUpdatedIntegrationEventHandler(ShoppingDbContext db)
    : INotificationHandler<ProductProjectionUpdatedIntegrationEvent>
{
    public async Task Handle(ProductProjectionUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var existing = await db.Products
            .FirstOrDefaultAsync(x => x.Id == notification.ProductId, cancellationToken);

        if (notification.IsDeleted)
        {
            if (existing is not null)
            {
                db.Products.Remove(existing);
                await db.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        if (existing is null)
        {
            existing = new ProductReadModel { Id = notification.ProductId };
            db.Products.Add(existing);
        }

        existing.IsActive = true;
        existing.Price = notification.Price;
        existing.UpdatedAt = notification.OccurredAt == default ? DateTime.UtcNow : notification.OccurredAt;

        await db.SaveChangesAsync(cancellationToken);
    }
}
