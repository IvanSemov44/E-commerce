namespace ECommerce.Identity.Application.Interfaces;

public interface IIdentityIntegrationEventPublisher
{
    Task PublishUserRegisteredAsync(Guid userId, string email, CancellationToken cancellationToken = default);
    Task PublishEmailVerifiedAsync(Guid userId, CancellationToken cancellationToken = default);
    Task PublishPasswordChangedAsync(Guid userId, CancellationToken cancellationToken = default);
}
