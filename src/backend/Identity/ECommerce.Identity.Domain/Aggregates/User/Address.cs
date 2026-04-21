using ECommerce.Identity.Domain.Errors;
using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Identity.Domain.Aggregates.User;

public sealed class Address : Entity
{
    public string Street { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string Country { get; private set; } = null!;
    public string? PostalCode { get; private set; }
    public bool IsDefaultShipping { get; private set; }
    public bool IsDefaultBilling { get; private set; }

    private Address() { } // EF Core

    internal Address(Guid id, string street, string city, string country, string? postalCode)
    {
        Id = id;
        (Street, City, Country, PostalCode) = (street, city, country, postalCode);
    }

    internal Address(Guid id, string street, string city, string country, string? postalCode,
        bool isDefaultShipping, bool isDefaultBilling) : this(id, street, city, country, postalCode)
    {
        IsDefaultShipping = isDefaultShipping;
        IsDefaultBilling = isDefaultBilling;
    }

    internal static Result<Address> Create(string street, string city, string country, string? postalCode)
    {
        if (string.IsNullOrWhiteSpace(street))  return Result<Address>.Fail(IdentityErrors.AddressStreetEmpty);
        if (string.IsNullOrWhiteSpace(city))    return Result<Address>.Fail(IdentityErrors.AddressCityEmpty);
        if (string.IsNullOrWhiteSpace(country)) return Result<Address>.Fail(IdentityErrors.AddressCountryEmpty);

        return Result<Address>.Ok(new Address(
            Guid.NewGuid(),
            street.Trim(), city.Trim(), country.Trim(), postalCode?.Trim()));
    }

    internal void SetDefaultShipping(bool value) => IsDefaultShipping = value;
    internal void SetDefaultBilling(bool value)  => IsDefaultBilling = value;
}
