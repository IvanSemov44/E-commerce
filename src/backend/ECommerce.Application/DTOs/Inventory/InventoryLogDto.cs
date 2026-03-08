namespace ECommerce.Application.DTOs.Inventory;

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
