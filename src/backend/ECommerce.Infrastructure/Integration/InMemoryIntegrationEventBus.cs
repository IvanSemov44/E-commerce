using ECommerce.Contracts;

namespace ECommerce.Infrastructure.Integration;

/// <summary>
/// Free in-process integration event bus used for local or fallback transport.
/// </summary>
public sealed class InMemoryIntegrationEventBus(IIntegrationEventDispatcher dispatcher) : IIntegrationEventBus
{
    public Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        => dispatcher.DispatchAsync(integrationEvent, cancellationToken);
}
