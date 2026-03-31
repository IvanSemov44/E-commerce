using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Domain.Aggregates.User;

namespace ECommerce.Identity.Application.Extensions;

/// <summary>
/// Maps domain User aggregate to application DTOs.
/// No AutoMapper needed — simple one-to-one mapping.
/// </summary>
public static class UserMappingExtensions
{
    public static UserProfileDto ToProfileDto(this User user) =>
        new(
            Id: user.Id,
            Email: user.Email.Value,
            FirstName: user.Name.First,
            LastName: user.Name.Last,
            PhoneNumber: user.PhoneNumber,
            Role: user.Role.ToString(),
            IsEmailVerified: user.IsEmailVerified,
            Addresses: user.Addresses.Select(a => new AddressDto(
                Id: a.Id,
                Street: a.Street,
                City: a.City,
                Country: a.Country,
                PostalCode: a.PostalCode,
                IsDefaultShipping: a.IsDefaultShipping,
                IsDefaultBilling: a.IsDefaultBilling
            )).ToList()
        );
}
