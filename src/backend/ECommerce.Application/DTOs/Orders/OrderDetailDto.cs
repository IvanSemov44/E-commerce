using ECommerce.Application.DTOs.Common;

namespace ECommerce.Application.DTOs.Orders;

public record OrderDetailDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string OrderNumber { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string PaymentStatus { get; init; } = null!;
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<OrderItemDto> Items { get; init; } = new();
    public string? PaymentMethod { get; init; }
    public decimal Subtotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal ShippingAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public AddressDto? ShippingAddress { get; init; }
    public AddressDto? BillingAddress { get; init; }
    public string? Notes { get; init; }
    public string? TrackingNumber { get; init; }
    public DateTime? ShippedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
}
