using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Events;
using MediatR;

namespace ECommerce.Identity.Infrastructure.EventHandlers;

public sealed class PasswordChangedEventHandler(IIdentityIntegrationEventPublisher publisher)
    : INotificationHandler<PasswordChangedEvent>
{
    public Task Handle(PasswordChangedEvent notification, CancellationToken cancellationToken)
        => publisher.PublishPasswordChangedAsync(notification.UserId, cancellationToken);
}
