using ECommerce.Contracts;
using ECommerce.Shopping.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Shopping.Infrastructure.IntegrationEvents;

public class InventoryStockProjectionUpdatedIntegrationEventHandler(
    ShoppingDbContext db,
    ILogger<InventoryStockProjectionUpdatedIntegrationEventHandler> logger)
    : INotificationHandler<InventoryStockProjectionUpdatedIntegrationEvent>
{
    public async Task Handle(InventoryStockProjectionUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        try
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

            logger.LogInformation(
                "Shopping stock projection upserted: ProductId={ProductId}, Quantity={Quantity}, IdempotencyKey={IdempotencyKey}",
                notification.ProductId,
                notification.Quantity,
                notification.IdempotencyKey);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(
                ex,
                "Shopping stock projection concurrency conflict: ProductId={ProductId}, IdempotencyKey={IdempotencyKey}",
                notification.ProductId,
                notification.IdempotencyKey);
            throw;
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(
                ex,
                "Shopping stock projection database update failed: ProductId={ProductId}, IdempotencyKey={IdempotencyKey}",
                notification.ProductId,
                notification.IdempotencyKey);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Shopping stock projection handler failed: ProductId={ProductId}, IdempotencyKey={IdempotencyKey}",
                notification.ProductId,
                notification.IdempotencyKey);
            throw;
        }
    }
}
