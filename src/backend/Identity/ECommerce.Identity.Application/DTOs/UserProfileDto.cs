namespace ECommerce.Identity.Application.DTOs;

public record UserProfileDto(
    Guid              Id,
    string            Email,
    string            FirstName,
    string            LastName,
    string?           PhoneNumber,
    string            Role,
    bool              IsEmailVerified,
    IReadOnlyList<AddressDto> Addresses
);
