namespace ECommerce.Contracts;

/// <summary>
/// Publishes integration events to the configured messaging transport.
/// </summary>
public interface IIntegrationEventBus
{
    Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
