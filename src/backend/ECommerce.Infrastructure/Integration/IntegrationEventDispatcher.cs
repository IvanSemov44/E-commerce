using ECommerce.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Integration;

public sealed class IntegrationEventDispatcher(
    InboxIdempotencyProcessor inbox,
    IPublisher mediator,
    ILogger<IntegrationEventDispatcher> logger) : IIntegrationEventDispatcher
{
    public async Task DispatchAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        await DispatchWithInboxAsync((dynamic)integrationEvent, cancellationToken);
    }

    private async Task DispatchWithInboxAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IntegrationEvent
    {
        await inbox.ExecuteAsync(
            integrationEvent,
            ct => mediator.Publish(integrationEvent, ct),
            cancellationToken);

        logger.LogDebug(
            "Processed integration event {EventType} with idempotency key {IdempotencyKey}",
            integrationEvent.GetType().Name,
            integrationEvent.IdempotencyKey);
    }
}
