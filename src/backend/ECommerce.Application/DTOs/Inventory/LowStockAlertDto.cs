namespace ECommerce.Application.DTOs.Inventory;

public record LowStockAlertDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public string? Sku { get; init; }
    public int CurrentStock { get; init; }
    public int LowStockThreshold { get; init; }
}
