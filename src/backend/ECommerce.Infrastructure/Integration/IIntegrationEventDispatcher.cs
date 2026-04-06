using ECommerce.Contracts;

namespace ECommerce.Infrastructure.Integration;

public interface IIntegrationEventDispatcher
{
    Task DispatchAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
