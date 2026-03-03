using ECommerce.Application.DTOs.Common;

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
    public DateTime? ShippedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
}

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

public class CreateOrderDto
{
    public List<CreateOrderItemDto> Items { get; set; } = new();
    public AddressDto ShippingAddress { get; set; } = null!;
    public AddressDto? BillingAddress { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PromoCode { get; set; }
    /// <summary>
    /// Email address for guest checkout. Required if not authenticated.
    /// </summary>
    public string? GuestEmail { get; set; }
}

public class CreateOrderItemDto
{
    public string ProductId { get; set; } = null!;
    public int Quantity { get; set; }
}

// AddressDto moved to ECommerce.Application.DTOs.Common/AddressDto.cs

/// <summary>
/// Request DTO for updating order status.
/// </summary>
public class UpdateOrderStatusDto
{
    public string Status { get; set; } = null!;
}
