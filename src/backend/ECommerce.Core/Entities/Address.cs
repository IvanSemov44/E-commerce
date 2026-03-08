using ECommerce.Core.Common;

namespace ECommerce.Core.Entities;

public class Address : BaseEntity
{
    /// <summary>
    /// User ID. Can be null for guest order addresses.
    /// </summary>
    public Guid? UserId { get; set; }
    public string Type { get; set; } = null!; // 'Shipping' or 'Billing'
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
    public bool IsDefault { get; set; }

    // Navigation property
    public virtual User? User { get; set; }
}
