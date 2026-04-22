using ECommerce.Identity.Domain.Events;

namespace ECommerce.Identity.Application.DomainEventHandlers;

public class AddressDefaultShippingChangedEventHandler(IAddressProjectionEventPublisher publisher)
    : INotificationHandler<AddressDefaultShippingChangedEvent>
{
    public Task Handle(AddressDefaultShippingChangedEvent notification, CancellationToken ct)
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
