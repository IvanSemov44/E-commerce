using ECommerce.Inventory.Application.Interfaces;
using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using ECommerce.Inventory.Domain.Interfaces;
using ECommerce.Ordering.Domain.Events;
using ECommerce.SharedKernel.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Inventory.Application.EventHandlers;

public class ReduceStockOnOrderPlacedHandler(
    IInventoryItemRepository inventory,
    IUnitOfWork uow,
    IInventoryProjectionEventPublisher projectionPublisher,
    IInventoryReservationEventPublisher reservationPublisher,
    ILogger<ReduceStockOnOrderPlacedHandler> logger) : INotificationHandler<OrderPlacedEvent>
{
    public async Task Handle(OrderPlacedEvent notification, CancellationToken ct)
    {
        try
        {
            var inventoryItems = new List<(Guid ProductId, int Quantity, InventoryItem Item)>();

            foreach (var item in notification.Items)
            {
                var inventoryItem = await inventory.GetByProductIdAsync(item.ProductId, ct);
                if (inventoryItem is null)
                {
                    logger.LogWarning("Inventory item not found for product {ProductId}", item.ProductId);
                    await reservationPublisher.PublishInventoryReservationFailedAsync(
                        notification.OrderId,
                        item.ProductId,
                        "Inventory item not found.",
                        ct);
                    return;
                }

                if (inventoryItem.Stock.Quantity < item.Quantity)
                {
                    logger.LogWarning(
                        "Insufficient stock for product {ProductId}. Requested {Requested} Available {Available}",
                        item.ProductId,
                        item.Quantity,
                        inventoryItem.Stock.Quantity);

                    await reservationPublisher.PublishInventoryReservationFailedAsync(
                        notification.OrderId,
                        item.ProductId,
                        "Insufficient stock.",
                        ct);
                    return;
                }

                inventoryItems.Add((item.ProductId, item.Quantity, inventoryItem));
            }

            foreach (var reservation in inventoryItems)
            {
                var reduceResult = reservation.Item.Reduce(reservation.Quantity, $"Order {notification.OrderId}");
                if (!reduceResult.IsSuccess)
                {
                    var error = reduceResult.GetErrorOrThrow();
                    logger.LogWarning(
                        "Failed reducing stock for product {ProductId}. Error code: {Code}",
                        reservation.ProductId,
                        error.Code);

                    await reservationPublisher.PublishInventoryReservationFailedAsync(
                        notification.OrderId,
                        reservation.ProductId,
                        error.Message,
                        ct);
                    return;
                }

                await inventory.UpdateAsync(reservation.Item, ct);
            }

            await uow.SaveChangesAsync(ct);

            foreach (var item in notification.Items)
            {
                var inventoryItem = await inventory.GetByProductIdAsync(item.ProductId, ct);
                if (inventoryItem is null)
                {
                    continue;
                }

                await projectionPublisher.PublishStockProjectionUpdatedAsync(
                    item.ProductId,
                    inventoryItem.Stock.Quantity,
                    $"Order {notification.OrderId}",
                    ct);
            }

            await reservationPublisher.PublishInventoryReservedAsync(
                notification.OrderId,
                notification.Items.Select(x => x.ProductId).ToArray(),
                notification.Items.Select(x => x.Quantity).ToArray(),
                ct);

            logger.LogInformation("Stock reduced for order {OrderId}", notification.OrderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reduce stock for order {OrderId}", notification.OrderId);

            var firstProductId = notification.Items.Count > 0
                ? notification.Items[0].ProductId
                : Guid.Empty;

            await reservationPublisher.PublishInventoryReservationFailedAsync(
                notification.OrderId,
                firstProductId,
                "Inventory processing failed due to an unexpected error.",
                ct);
        }
    }
}
