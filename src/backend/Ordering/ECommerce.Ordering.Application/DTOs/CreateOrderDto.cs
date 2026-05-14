using ECommerce.Contracts.DTOs.Common;

namespace ECommerce.Ordering.Application.DTOs;

public class CreateOrderDto
{
    public List<CreateOrderItemDto> Items { get; set; } = new();
    public AddressDto ShippingAddress { get; set; } = null!;
    public AddressDto? BillingAddress { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PromoCode { get; set; }
    public string? GuestEmail { get; set; }
}
