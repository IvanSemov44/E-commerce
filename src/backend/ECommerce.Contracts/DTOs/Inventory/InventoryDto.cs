namespace ECommerce.Contracts.DTOs.Inventory;

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

