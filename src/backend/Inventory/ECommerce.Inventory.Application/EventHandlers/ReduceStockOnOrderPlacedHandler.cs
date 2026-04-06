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
    private readonly ILogger<ReduceStockOnOrderPlacedHandler> _logger;

    public ReduceStockOnOrderPlacedHandler(
        IInventoryItemRepository inventory,
        IUnitOfWork uow,
        IInventoryProjectionEventPublisher projectionPublisher,
        ILogger<ReduceStockOnOrderPlacedHandler> logger)
    {
        _inventory = inventory;
        _uow = uow;
        _projectionPublisher = projectionPublisher;
        _logger = logger;
    }

    public async Task Handle(OrderPlacedEvent notification, CancellationToken ct)
    {
        try
        {
            foreach (var item in notification.Items)
            {
                var inventoryItem = await _inventory.GetByProductIdAsync(item.ProductId, ct);
                if (inventoryItem is null)
                {
                    _logger.LogWarning("Inventory item not found for product {ProductId}", item.ProductId);
                    continue;
                }

                inventoryItem.Reduce(item.Quantity, $"Order {notification.OrderId}");
                await _inventory.UpdateAsync(inventoryItem, ct);
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

            _logger.LogInformation("Stock reduced for order {OrderId}", notification.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reduce stock for order {OrderId}", notification.OrderId);
        }
    }
}
