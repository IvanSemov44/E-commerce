using System.Text.Json;
using ECommerce.Contracts;
using ECommerce.Promotions.Infrastructure.Data;
using ECommerce.Promotions.Infrastructure.Persistence;

namespace ECommerce.Promotions.Infrastructure.Integration;

/// <summary>
/// Persists Promotions integration events into the promotions.outbox_messages table.
/// </summary>
public sealed class EfPromotionsIntegrationEventOutbox(PromotionsDbContext dbContext) : IIntegrationEventOutbox
{
    public Task EnqueueAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = integrationEvent.IdempotencyKey,
            EventType = integrationEvent.GetType().AssemblyQualifiedName
                        ?? integrationEvent.GetType().FullName
                        ?? integrationEvent.GetType().Name,
            EventData = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.OutboxMessages.Add(outboxMessage);
        return Task.CompletedTask;
    }
}
