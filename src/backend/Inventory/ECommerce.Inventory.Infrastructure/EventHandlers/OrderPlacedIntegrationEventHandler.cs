using ECommerce.Contracts;
using ECommerce.Inventory.Application.Interfaces;
using ECommerce.Inventory.Domain.Interfaces;
using ECommerce.Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Inventory.Infrastructure.EventHandlers;

public sealed class OrderPlacedIntegrationEventHandler(
    IInventoryItemRepository inventory,
    IInventoryReservationEventPublisher reservationPublisher,
    InventoryDbContext db,
    ILogger<OrderPlacedIntegrationEventHandler> logger)
    : INotificationHandler<OrderPlacedIntegrationEvent>
{
    public async Task Handle(OrderPlacedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var existing = await db.InboxMessages
            .FirstOrDefaultAsync(m => m.IdempotencyKey == notification.IdempotencyKey, cancellationToken);

        if (existing?.ProcessedAt != null)
        {
            logger.LogInformation(
                "Skipping duplicate OrderPlaced event for OrderId {OrderId}", notification.OrderId);
            return;
        }

        var quantities = notification.Quantities;
        if (quantities.Length != notification.ProductIds.Length)
        {
            logger.LogWarning(
                "OrderPlaced quantities/productIds mismatch for OrderId {OrderId}", notification.OrderId);
            await reservationPublisher.PublishInventoryReservationFailedAsync(
                notification.OrderId,
                notification.ProductIds.FirstOrDefault(),
                "Malformed order: quantities and product IDs count mismatch.",
                cancellationToken);
            await CommitInboxAsync(existing, notification, cancellationToken);
            return;
        }

        var itemPairs = notification.ProductIds
            .Zip(quantities, (productId, qty) => (ProductId: productId, Quantity: qty))
            .ToList();

        var reservations = new List<(Guid ProductId, int Quantity, Domain.Aggregates.InventoryItem.InventoryItem Item)>();

        foreach (var (productId, quantity) in itemPairs)
        {
            var item = await inventory.GetByProductIdAsync(productId, cancellationToken);
            if (item is null)
            {
                logger.LogWarning("Inventory not found for ProductId {ProductId}", productId);
                await reservationPublisher.PublishInventoryReservationFailedAsync(
                    notification.OrderId, productId, "Inventory item not found.", cancellationToken);
                await CommitInboxAsync(existing, notification, cancellationToken);
                return;
            }

            if (item.Stock.Quantity < quantity)
            {
                logger.LogWarning(
                    "Insufficient stock for ProductId {ProductId}: requested {Requested}, available {Available}",
                    productId, quantity, item.Stock.Quantity);
                await reservationPublisher.PublishInventoryReservationFailedAsync(
                    notification.OrderId, productId, "Insufficient stock.", cancellationToken);
                await CommitInboxAsync(existing, notification, cancellationToken);
                return;
            }

            reservations.Add((productId, quantity, item));
        }

        foreach (var (_, quantity, item) in reservations)
        {
            var reduceResult = item.Reduce(quantity, $"Order {notification.OrderId}");
            if (!reduceResult.IsSuccess)
            {
                var error = reduceResult.GetErrorOrThrow();
                logger.LogWarning(
                    "Failed reducing stock for ProductId {ProductId}: {Code}",
                    item.ProductId, error.Code);
                await reservationPublisher.PublishInventoryReservationFailedAsync(
                    notification.OrderId, item.ProductId, error.Message, cancellationToken);
                await CommitInboxAsync(existing, notification, cancellationToken);
                return;
            }
        }

        await reservationPublisher.PublishInventoryReservedAsync(
            notification.OrderId,
            notification.ProductIds,
            notification.Quantities,
            cancellationToken);

        logger.LogInformation("Stock reserved for OrderId {OrderId}", notification.OrderId);

        await CommitInboxAsync(existing, notification, cancellationToken);
    }

    private async Task CommitInboxAsync(
        InboxMessage? existing,
        OrderPlacedIntegrationEvent notification,
        CancellationToken cancellationToken)
    {
        if (existing is null)
        {
            db.InboxMessages.Add(new InboxMessage
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = notification.IdempotencyKey,
                EventType = typeof(OrderPlacedIntegrationEvent).AssemblyQualifiedName!,
                ReceivedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow,
                AttemptCount = 1
            });
        }
        else
        {
            existing.ProcessedAt = DateTime.UtcNow;
            existing.AttemptCount += 1;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
