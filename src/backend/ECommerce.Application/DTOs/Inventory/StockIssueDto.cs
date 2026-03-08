namespace ECommerce.Application.DTOs.Inventory;

public record StockIssueDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public int RequestedQuantity { get; init; }
    public int AvailableQuantity { get; init; }
    public string Message { get; init; } = null!;
}
