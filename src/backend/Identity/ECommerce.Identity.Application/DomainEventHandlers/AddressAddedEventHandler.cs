using ECommerce.Identity.Domain.Events;

namespace ECommerce.Identity.Application.DomainEventHandlers;

public class AddressAddedEventHandler(IAddressProjectionEventPublisher publisher)
    : INotificationHandler<AddressAddedEvent>
{
    public Task Handle(AddressAddedEvent notification, CancellationToken ct)
        => publisher.PublishAddressProjectionUpdatedAsync(
            notification.AddressId,
            notification.UserId,
            notification.Street,
            notification.City,
            notification.Country,
            notification.PostalCode ?? string.Empty,
            isDeleted: false,
            ct);
}
