namespace ECommerce.Reviews.Infrastructure.Persistence;

/// <summary>
/// Stores permanently failed integration events for operational recovery.
/// </summary>
public class DeadLetterMessage
{
    public Guid Id { get; set; }

    public Guid OutboxMessageId { get; set; }

    public Guid IdempotencyKey { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string EventData { get; set; } = string.Empty;

    public int RetryCount { get; set; }

    public string? LastError { get; set; }

    public DateTime FailedAt { get; set; }

    public DateTime? RequeuedAt { get; set; }
}
