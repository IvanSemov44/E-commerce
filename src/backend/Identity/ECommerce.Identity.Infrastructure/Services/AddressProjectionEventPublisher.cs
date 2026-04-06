using ECommerce.Contracts;
using ECommerce.Identity.Application.Interfaces;

namespace ECommerce.Identity.Infrastructure.Services;

public sealed class AddressProjectionEventPublisher(IIntegrationEventOutbox outbox) : IAddressProjectionEventPublisher
{
    public Task PublishAddressProjectionUpdatedAsync(
        Guid addressId,
        Guid userId,
        string streetLine1,
        string city,
        string country,
        string postalCode,
        bool isDeleted,
        CancellationToken cancellationToken = default)
    {
        var integrationEvent = new AddressProjectionUpdatedIntegrationEvent(
            addressId,
            userId,
            streetLine1,
            city,
            country,
            postalCode,
            isDeleted,
            DateTime.UtcNow);

        return outbox.EnqueueAsync(integrationEvent, cancellationToken);
    }
}
