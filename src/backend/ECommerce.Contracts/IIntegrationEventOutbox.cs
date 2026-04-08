namespace ECommerce.Contracts;

/// <summary>
/// Queues integration events for asynchronous outbox dispatch.
/// </summary>
public interface IIntegrationEventOutbox
{
    Task EnqueueAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;
}
