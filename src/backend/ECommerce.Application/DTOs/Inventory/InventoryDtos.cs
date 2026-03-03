namespace ECommerce.Application.DTOs.Inventory;

public record InventoryDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public string? Sku { get; init; }
    public int StockQuantity { get; init; }
    public int LowStockThreshold { get; init; }
    public string? ImageUrl { get; init; }
    public decimal Price { get; init; }
    public bool IsLowStock => StockQuantity <= LowStockThreshold;
    public bool IsOutOfStock => StockQuantity <= 0;
}

public record InventoryLogDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public int QuantityChange { get; init; }
    public int StockAfterChange { get; init; }
    public string Reason { get; init; } = null!;
    public Guid? ReferenceId { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedByUserName { get; init; }
}

public class AdjustStockRequest
{
    public int Quantity { get; set; }
    public string Reason { get; set; } = null!; // 'restock', 'adjustment', 'damage', 'correction'
    public string? Notes { get; set; }
}

public class StockCheckRequest
{
    public List<StockCheckItemDto> Items { get; set; } = new();
}

public class StockCheckItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public record StockCheckResponse
{
    public bool IsAvailable { get; init; }
    public List<StockIssueDto> Issues { get; init; } = new();
}

public record StockIssueDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public int RequestedQuantity { get; init; }
    public int AvailableQuantity { get; init; }
    public string Message { get; init; } = null!;
}

public record LowStockAlertDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public string? Sku { get; init; }
    public int CurrentStock { get; init; }
    public int LowStockThreshold { get; init; }
}

public record StockAdjustmentResponseDto
{
    public Guid ProductId { get; init; }
    public int NewQuantity { get; init; }
    public int QuantityChanged { get; init; }
    public DateTime AdjustedAt { get; init; }
}

public class BulkStockUpdateRequest
{
    public List<BulkStockUpdateItem> Updates { get; set; } = new();
}

public class BulkStockUpdateItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
