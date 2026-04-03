namespace ECommerce.Inventory.Application.DTOs;

public record InventoryItemDto(
    Guid Id,
    Guid ProductId,
    int Quantity,
    int LowStockThreshold,
    bool IsLowStock,
    bool IsOutOfStock
);