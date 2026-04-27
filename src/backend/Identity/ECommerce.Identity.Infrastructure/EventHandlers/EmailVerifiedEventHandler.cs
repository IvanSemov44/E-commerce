using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Events;
using MediatR;

namespace ECommerce.Identity.Infrastructure.EventHandlers;

public sealed class EmailVerifiedEventHandler(IIdentityIntegrationEventPublisher publisher)
    : INotificationHandler<EmailVerifiedEvent>
{
    public Task Handle(EmailVerifiedEvent notification, CancellationToken cancellationToken)
        => publisher.PublishEmailVerifiedAsync(notification.UserId, cancellationToken);
}
