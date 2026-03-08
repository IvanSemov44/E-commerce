namespace ECommerce.Application.DTOs.Cart;

public record CartItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public string? ProductImage { get; init; }
    public decimal Price { get; init; }
    public int Quantity { get; init; }
    public decimal Total { get; init; }
}
