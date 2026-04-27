using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Events;
using MediatR;

namespace ECommerce.Identity.Infrastructure.EventHandlers;

public sealed class AddressAddedEventHandler(IAddressProjectionEventPublisher publisher)
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
