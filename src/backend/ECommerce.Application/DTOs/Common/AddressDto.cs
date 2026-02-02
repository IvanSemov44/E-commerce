namespace ECommerce.Application.DTOs;

public class AddressDto
{
    public Guid? Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Company { get; set; }
    public string StreetLine1 { get; set; } = null!;
    public string? StreetLine2 { get; set; }
    public string City { get; set; } = null!;
    public string State { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
    public string Country { get; set; } = null!;
    public string? Phone { get; set; }
}
