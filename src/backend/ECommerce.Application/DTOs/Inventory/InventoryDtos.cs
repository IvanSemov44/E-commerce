namespace ECommerce.Application.DTOs.Inventory;

public class InventoryDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? Sku { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; }
    public bool IsLowStock => StockQuantity <= LowStockThreshold;
    public bool IsOutOfStock => StockQuantity <= 0;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
}

public class InventoryLogDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int QuantityChange { get; set; }
    public int StockAfterChange { get; set; }
    public string Reason { get; set; } = null!;
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByUserName { get; set; }
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

public class StockCheckResponse
{
    public bool IsAvailable { get; set; }
    public List<StockIssueDto> Issues { get; set; } = new();
}

public class StockIssueDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int RequestedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public string Message { get; set; } = null!;
}

public class LowStockAlertDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? Sku { get; set; }
    public int CurrentStock { get; set; }
    public int LowStockThreshold { get; set; }
}

public class StockAdjustmentResponseDto
{
    public Guid ProductId { get; set; }
    public int NewQuantity { get; set; }
    public int QuantityChanged { get; set; }
    public DateTime AdjustedAt { get; set; }
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
