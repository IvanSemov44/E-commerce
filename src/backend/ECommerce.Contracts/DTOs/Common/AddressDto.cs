namespace ECommerce.Contracts.DTOs.Common;

public record AddressDto
{
    public Guid? Id { get; init; }
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string? Company { get; init; }
    public string StreetLine1 { get; init; } = null!;
    public string? StreetLine2 { get; init; }
    public string City { get; init; } = null!;
    public string State { get; init; } = null!;
    public string PostalCode { get; init; } = null!;
    public string Country { get; init; } = null!;
    public string? Phone { get; init; }
}

