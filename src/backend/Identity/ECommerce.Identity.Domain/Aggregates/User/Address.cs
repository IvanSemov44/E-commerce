using ECommerce.SharedKernel.Domain;

namespace ECommerce.Identity.Domain.Aggregates.User;

// Child entity — NOT a value object. A user has many addresses.
// Each address has identity: you can say "delete THIS address."
// Only User.AddAddress() can create addresses (constructor is internal).
public sealed class Address : Entity
{
    public string  Street    { get; private set; } = null!;
    public string  City      { get; private set; } = null!;
    public string  Country   { get; private set; } = null!;
    public string? PostalCode { get; private set; }
    public bool IsDefaultShipping { get; private set; }
    public bool IsDefaultBilling  { get; private set; }

    private Address() { } // EF Core

    internal Address(Guid id, string street, string city, string country, string? postalCode)
    {
        Id          = id;
        Street      = street;
        City        = city;
        Country     = country;
        PostalCode  = postalCode;
    }

    internal void SetDefaultShipping(bool value) => IsDefaultShipping = value;
    internal void SetDefaultBilling(bool value)  => IsDefaultBilling  = value;
}
