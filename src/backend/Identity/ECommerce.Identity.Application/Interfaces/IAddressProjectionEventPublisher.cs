namespace ECommerce.Identity.Application.Interfaces;

public interface IAddressProjectionEventPublisher
{
    Task PublishAddressProjectionUpdatedAsync(
        Guid addressId,
        Guid userId,
        string streetLine1,
        string city,
        string country,
        string postalCode,
        bool isDeleted,
        CancellationToken cancellationToken = default);
}
