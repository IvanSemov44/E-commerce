using System.Text.Json;
using ECommerce.Contracts;

namespace ECommerce.Identity.Infrastructure.Integration;

public sealed class IdentityOutboxEventWriter(IdentityDbContext dbContext) : IIdentityOutboxEventWriter
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public Task EnqueueAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = integrationEvent.IdempotencyKey,
            EventType = integrationEvent.GetType().AssemblyQualifiedName ?? integrationEvent.GetType().FullName!,
            EventData = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), _json),
            CreatedAt = DateTime.UtcNow
        });

        return Task.CompletedTask;
    }
}
