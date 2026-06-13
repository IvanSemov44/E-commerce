using ECommerce.SharedKernel.Domain;

namespace ECommerce.Ordering.Domain.ValueObjects;

public sealed class ShippingAddress : ValueObject
{
    public string Street { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string Country { get; private set; } = null!;
    public string? PostalCode { get; private set; }

    private ShippingAddress() { }
    private ShippingAddress(string street, string city, string country, string? postalCode)
    {
        Street = street;
        City = city;
        Country = country;
        PostalCode = postalCode;
    }

    public static ShippingAddress Create(string street, string city, string country, string? postalCode)
        => new(street, city, country, postalCode);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return Country;
        yield return PostalCode;
    }
}
