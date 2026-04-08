namespace ECommerce.SharedKernel.DTOs;

/// <summary>
/// Data needed to render an order confirmation / shipped / delivered email.
/// Decouples IEmailService from SharedKernel.Entities.Order.
/// </summary>
public record OrderEmailDto(
    string OrderNumber,
    DateTime CreatedAt,
    string Status,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal ShippingAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    IEnumerable<OrderItemEmailDto> Items,
    AddressEmailDto? ShippingAddress
);

public record OrderItemEmailDto(
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);

public record AddressEmailDto(
    string? FirstName,
    string? LastName,
    string? StreetLine1,
    string? StreetLine2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country
);

/// <summary>
/// Data needed to render an abandoned cart email.
/// Decouples IEmailService from SharedKernel.Entities.Cart.
/// </summary>
public record CartEmailDto(
    IEnumerable<CartItemEmailDto> Items
);

public record CartItemEmailDto(
    string ProductName,
    int Quantity,
    decimal Price
);
