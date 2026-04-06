using ECommerce.Contracts;
using MassTransit;

namespace ECommerce.Infrastructure.Integration;

/// <summary>
/// Publishes integration events through MassTransit transport.
/// </summary>
public sealed class MassTransitIntegrationEventBus(IPublishEndpoint publishEndpoint) : IIntegrationEventBus
{
    public Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        => publishEndpoint.Publish(integrationEvent, integrationEvent.GetType(), cancellationToken);
}
