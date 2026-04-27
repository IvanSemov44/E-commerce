using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Events;
using MediatR;

namespace ECommerce.Identity.Infrastructure.EventHandlers;

public sealed class AddressDefaultShippingChangedEventHandler(IAddressProjectionEventPublisher publisher)
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
