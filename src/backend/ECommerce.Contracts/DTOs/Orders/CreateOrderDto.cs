using ECommerce.Contracts.DTOs.Common;

namespace ECommerce.Contracts.DTOs.Orders;

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

