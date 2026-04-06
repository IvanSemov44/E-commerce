using ECommerce.Contracts;
using ECommerce.Shopping.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shopping.Infrastructure.IntegrationEvents;

public class InventoryStockProjectionUpdatedIntegrationEventHandler(ShoppingDbContext db)
    : INotificationHandler<InventoryStockProjectionUpdatedIntegrationEvent>
{
    public async Task Handle(InventoryStockProjectionUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var existing = await db.InventoryItems
            .FirstOrDefaultAsync(x => x.ProductId == notification.ProductId, cancellationToken);

        if (existing is null)
        {
            db.InventoryItems.Add(new InventoryItemReadModel
            {
                ProductId = notification.ProductId,
                Quantity = notification.Quantity,
                UpdatedAt = notification.OccurredAt == default ? DateTime.UtcNow : notification.OccurredAt
            });
        }
        else
        {
            existing.Quantity = notification.Quantity;
            existing.UpdatedAt = notification.OccurredAt == default ? DateTime.UtcNow : notification.OccurredAt;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
