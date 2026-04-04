namespace ECommerce.Shopping.Application.DTOs;

public record CartItemDto(
    Guid    Id,
    Guid    ProductId,
    int     Quantity,
    decimal UnitPrice,
    string  Currency,
    decimal LineTotal
);