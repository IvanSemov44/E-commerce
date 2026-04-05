namespace ECommerce.Ordering.Application.DTOs;

public record OrderItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public string? ProductImageUrl { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
}
