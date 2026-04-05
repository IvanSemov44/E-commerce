namespace ECommerce.Ordering.Application.DTOs;

public record OrderDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string OrderNumber { get; init; } = null!;
    public string Status { get; init; } = null!;
    public decimal Total { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<OrderItemDto> Items { get; init; } = new();
}
