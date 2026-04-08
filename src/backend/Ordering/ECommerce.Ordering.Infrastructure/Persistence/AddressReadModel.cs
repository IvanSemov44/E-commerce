namespace ECommerce.Ordering.Infrastructure.Persistence;

public class AddressReadModel
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string StreetLine1 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
