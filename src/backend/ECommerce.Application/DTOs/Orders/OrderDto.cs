namespace ECommerce.Application.DTOs.Orders;

public record OrderDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string OrderNumber { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string PaymentStatus { get; init; } = null!;
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<OrderItemDto> Items { get; init; } = new();
}
