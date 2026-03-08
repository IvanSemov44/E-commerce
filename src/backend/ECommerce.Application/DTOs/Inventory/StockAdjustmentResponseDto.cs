namespace ECommerce.Application.DTOs.Inventory;

public record StockAdjustmentResponseDto
{
    public Guid ProductId { get; init; }
    public int NewQuantity { get; init; }
    public int QuantityChanged { get; init; }
    public DateTime AdjustedAt { get; init; }
}
