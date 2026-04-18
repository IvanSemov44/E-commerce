using MediatR;

namespace ECommerce.Contracts;

/// <summary>
/// Base contract for integration events exchanged across bounded contexts.
/// </summary>
public abstract record IntegrationEvent : INotification
{
    public Guid CorrelationId { get; init; } = Guid.NewGuid();

    public Guid IdempotencyKey { get; init; } = Guid.NewGuid();

    public DateTime PublishedAt { get; init; } = DateTime.UtcNow;
}
