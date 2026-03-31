namespace ECommerce.Identity.Application.DTOs;

public record AddressDto(
    Guid    Id,
    string  Street,
    string  City,
    string  Country,
    string? PostalCode,
    bool    IsDefaultShipping,
    bool    IsDefaultBilling
);
