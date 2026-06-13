namespace ECommerce.Inventory.Application.DTOs;

public record StockAvailabilityIssueDto(
    Guid ProductId,
    int Available,
    int Requested,
    string Message
);

public record BulkStockAvailabilityDto(
    bool IsAvailable,
    IReadOnlyList<StockAvailabilityIssueDto> Issues
);
