using MediatR;
using ECommerce.Inventory.Domain.Interfaces;
using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using ECommerce.Inventory.Application.Interfaces;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Ordering.Domain.Events;
using Microsoft.Extensions.Logging;

namespace ECommerce.Inventory.Application.EventHandlers;

public class ReduceStockOnOrderPlacedHandler : INotificationHandler<OrderPlacedEvent>
{
    private readonly IInventoryItemRepository _inventory;
    private readonly IUnitOfWork _uow;
    private readonly IInventoryProjectionEventPublisher _projectionPublisher;
    private readonly IInventoryReservationEventPublisher _reservationPublisher;
    private readonly ILogger<ReduceStockOnOrderPlacedHandler> _logger;

    public ReduceStockOnOrderPlacedHandler(
        IInventoryItemRepository inventory,
        IUnitOfWork uow,
        IInventoryProjectionEventPublisher projectionPublisher,
        IInventoryReservationEventPublisher reservationPublisher,
        ILogger<ReduceStockOnOrderPlacedHandler> logger)
    {
        _inventory = inventory;
        _uow = uow;
        _projectionPublisher = projectionPublisher;
        _reservationPublisher = reservationPublisher;
        _logger = logger;
    }

    public async Task Handle(OrderPlacedEvent notification, CancellationToken ct)
    {
        try
        {
            var inventoryItems = new List<(Guid ProductId, int Quantity, InventoryItem Item)>();

            foreach (var item in notification.Items)
            {
                var inventoryItem = await _inventory.GetByProductIdAsync(item.ProductId, ct);
                if (inventoryItem is null)
                {
                    _logger.LogWarning("Inventory item not found for product {ProductId}", item.ProductId);
                    await _reservationPublisher.PublishInventoryReservationFailedAsync(
                        notification.OrderId,
                        item.ProductId,
                        "Inventory item not found.",
                        ct);
                    return;
                }

                if (inventoryItem.Stock.Quantity < item.Quantity)
                {
                    _logger.LogWarning(
                        "Insufficient stock for product {ProductId}. Requested {Requested} Available {Available}",
                        item.ProductId,
                        item.Quantity,
                        inventoryItem.Stock.Quantity);

                    await _reservationPublisher.PublishInventoryReservationFailedAsync(
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
                    _logger.LogWarning(
                        "Failed reducing stock for product {ProductId}. Error code: {Code}",
                        reservation.ProductId,
                        error.Code);

                    await _reservationPublisher.PublishInventoryReservationFailedAsync(
                        notification.OrderId,
                        reservation.ProductId,
                        error.Message,
                        ct);
                    return;
                }

                await _inventory.UpdateAsync(reservation.Item, ct);
            }

            await _uow.SaveChangesAsync(ct);

            foreach (var item in notification.Items)
            {
                var inventoryItem = await _inventory.GetByProductIdAsync(item.ProductId, ct);
                if (inventoryItem is null)
                {
                    continue;
                }

                await _projectionPublisher.PublishStockProjectionUpdatedAsync(
                    item.ProductId,
                    inventoryItem.Stock.Quantity,
                    $"Order {notification.OrderId}",
                    ct);
            }

            await _reservationPublisher.PublishInventoryReservedAsync(
                notification.OrderId,
                notification.Items.Select(x => x.ProductId).ToArray(),
                notification.Items.Select(x => x.Quantity).ToArray(),
                ct);

            _logger.LogInformation("Stock reduced for order {OrderId}", notification.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reduce stock for order {OrderId}", notification.OrderId);

            var firstProductId = notification.Items.Count > 0
                ? notification.Items[0].ProductId
                : Guid.Empty;

            await _reservationPublisher.PublishInventoryReservationFailedAsync(
                notification.OrderId,
                firstProductId,
                "Inventory processing failed due to an unexpected error.",
                ct);
        }
    }
}
