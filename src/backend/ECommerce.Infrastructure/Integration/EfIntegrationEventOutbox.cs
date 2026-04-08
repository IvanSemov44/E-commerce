using System.Text.Json;
using ECommerce.Contracts;
using ECommerce.Infrastructure.Data;

namespace ECommerce.Infrastructure.Integration;

/// <summary>
/// Persists integration events into the relational outbox table.
/// </summary>
public sealed class EfIntegrationEventOutbox(AppDbContext dbContext) : IIntegrationEventOutbox
{
    public Task EnqueueAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = integrationEvent.IdempotencyKey,
            EventType = integrationEvent.GetType().AssemblyQualifiedName ?? integrationEvent.GetType().FullName ?? integrationEvent.GetType().Name,
            EventData = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Set<OutboxMessage>().Add(outboxMessage);
        return Task.CompletedTask;
    }
}
