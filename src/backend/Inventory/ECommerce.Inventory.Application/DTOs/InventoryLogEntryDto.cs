namespace ECommerce.Inventory.Application.DTOs;

public record InventoryLogEntryDto(
    int Delta,
    string Reason,
    int StockAfter,
    DateTime OccurredAt
);