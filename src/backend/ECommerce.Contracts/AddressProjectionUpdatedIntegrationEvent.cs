namespace ECommerce.Contracts;

/// <summary>
/// Published when an identity address projection changes and downstream read models should sync.
/// </summary>
public record AddressProjectionUpdatedIntegrationEvent(
    Guid AddressId,
    Guid UserId,
    string StreetLine1,
    string City,
    string Country,
    string PostalCode,
    bool IsDeleted,
    DateTime OccurredAt = default)
    : IntegrationEvent
{
    public AddressProjectionUpdatedIntegrationEvent()
        : this(Guid.Empty, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false)
    {
    }
}
