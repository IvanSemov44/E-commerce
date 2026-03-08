namespace ECommerce.Application.DTOs.Cart;

public record CartDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string? SessionId { get; init; }
    public List<CartItemDto> Items { get; init; } = new();
    public decimal Subtotal { get; init; }
    public decimal Total { get; init; }
}
