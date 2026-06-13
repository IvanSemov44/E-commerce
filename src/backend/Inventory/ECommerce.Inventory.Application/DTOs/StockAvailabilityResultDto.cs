namespace ECommerce.Inventory.Application.DTOs;

public record StockAvailabilityResultDto(
    Guid ProductId,
    int RequestedQuantity,
    bool IsAvailable
);
