using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Events;
using MediatR;

namespace ECommerce.Identity.Infrastructure.EventHandlers;

public sealed class UserRegisteredEventHandler(IIdentityIntegrationEventPublisher publisher)
    : INotificationHandler<UserRegisteredEvent>
{
    public Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
        => publisher.PublishUserRegisteredAsync(notification.UserId, notification.Email, cancellationToken);
}
