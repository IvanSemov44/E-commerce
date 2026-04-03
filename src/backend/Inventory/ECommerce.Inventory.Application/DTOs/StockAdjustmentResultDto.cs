namespace ECommerce.Inventory.Application.DTOs;

public record StockAdjustmentResultDto(
    Guid ProductId,
    int NewQuantity,
    int QuantityChanged,
    DateTime AdjustedAt
);