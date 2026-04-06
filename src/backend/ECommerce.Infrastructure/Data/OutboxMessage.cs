namespace ECommerce.Infrastructure.Data;

/// <summary>
/// Stores integration events until they are dispatched by the outbox worker.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }

    public Guid IdempotencyKey { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string EventData { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public int RetryCount { get; set; }

    public string? LastError { get; set; }
}
