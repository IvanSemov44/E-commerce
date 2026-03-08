namespace ECommerce.Application.DTOs.Orders;

public record OrderItemDto
{
    public Guid Id { get; init; }
    public string ProductName { get; init; } = null!;
    public string? ProductSku { get; init; }
    public string? ProductImageUrl { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
}
