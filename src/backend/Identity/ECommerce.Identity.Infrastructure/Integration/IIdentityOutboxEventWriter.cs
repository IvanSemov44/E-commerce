using ECommerce.Contracts;

namespace ECommerce.Identity.Infrastructure.Integration;

public interface IIdentityOutboxEventWriter
{
    Task EnqueueAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
