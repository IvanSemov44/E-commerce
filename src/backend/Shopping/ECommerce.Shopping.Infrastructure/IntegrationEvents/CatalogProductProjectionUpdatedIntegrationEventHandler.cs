using ECommerce.Contracts;
using ECommerce.Shopping.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Shopping.Infrastructure.IntegrationEvents;

public sealed class CatalogProductProjectionUpdatedIntegrationEventHandler(
    ShoppingDbContext db,
    ILogger<CatalogProductProjectionUpdatedIntegrationEventHandler> logger)
    : INotificationHandler<ProductProjectionUpdatedIntegrationEvent>
{
    public async Task Handle(ProductProjectionUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        try
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

                logger.LogInformation(
                    "Shopping product projection removed: ProductId={ProductId}, IdempotencyKey={IdempotencyKey}",
                    notification.ProductId,
                    notification.IdempotencyKey);

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

            logger.LogInformation(
                "Shopping product projection upserted: ProductId={ProductId}, IdempotencyKey={IdempotencyKey}",
                notification.ProductId,
                notification.IdempotencyKey);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(
                ex,
                "Shopping product projection concurrency conflict: ProductId={ProductId}, IdempotencyKey={IdempotencyKey}",
                notification.ProductId,
                notification.IdempotencyKey);
            throw;
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(
                ex,
                "Shopping product projection database update failed: ProductId={ProductId}, IdempotencyKey={IdempotencyKey}",
                notification.ProductId,
                notification.IdempotencyKey);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Shopping product projection handler failed: ProductId={ProductId}, IdempotencyKey={IdempotencyKey}",
                notification.ProductId,
                notification.IdempotencyKey);
            throw;
        }
    }
}
