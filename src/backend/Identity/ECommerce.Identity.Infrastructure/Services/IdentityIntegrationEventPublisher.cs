using ECommerce.Contracts;
using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Infrastructure.Integration;

namespace ECommerce.Identity.Infrastructure.Services;

public sealed class IdentityIntegrationEventPublisher(IIdentityOutboxEventWriter outbox)
    : IIdentityIntegrationEventPublisher
{
    public Task PublishUserRegisteredAsync(Guid userId, string email, CancellationToken cancellationToken = default)
        => outbox.EnqueueAsync(new UserRegisteredIntegrationEvent(userId, email, DateTime.UtcNow), cancellationToken);

    public Task PublishEmailVerifiedAsync(Guid userId, CancellationToken cancellationToken = default)
        => outbox.EnqueueAsync(new EmailVerifiedIntegrationEvent(userId, DateTime.UtcNow), cancellationToken);

    public Task PublishPasswordChangedAsync(Guid userId, CancellationToken cancellationToken = default)
        => outbox.EnqueueAsync(new PasswordChangedIntegrationEvent(userId, DateTime.UtcNow), cancellationToken);
}
