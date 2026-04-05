using ECommerce.Ordering.Application.DTOs;
using ECommerce.Ordering.Domain.Aggregates.Order;
using ECommerce.Ordering.Domain.ValueObjects;

namespace ECommerce.Ordering.Application.Mapping;

public static class OrderingMappingExtensions
{
    public static OrderDto ToDto(this Order order) => new()
    {
        Id = order.Id,
        UserId = order.UserId,
        OrderNumber = order.OrderNumber,
        Status = order.Status.Name,
        Total = order.Total,
        CreatedAt = DateTime.UtcNow,
        Items = order.Items.Select(ToDto).ToList()
    };

    public static OrderItemDto ToDto(this OrderItem item) => new()
    {
        Id = item.Id,
        ProductId = item.ProductId,
        ProductName = item.ProductName,
        ProductImageUrl = item.ProductImageUrl,
        Quantity = item.Quantity,
        UnitPrice = item.UnitPrice,
        TotalPrice = item.UnitPrice * item.Quantity
    };

    public static ShippingAddressDto ToDto(this ShippingAddress address) => new()
    {
        Street = address.Street,
        City = address.City,
        Country = address.Country,
        PostalCode = address.PostalCode
    };
}

public class ShippingAddressDto
{
    public string Street { get; init; } = null!;
    public string City { get; init; } = null!;
    public string Country { get; init; } = null!;
    public string? PostalCode { get; init; }
}
