namespace ECommerce.Payments.Infrastructure.Persistence;

public sealed class DeadLetterMessage
{
    public Guid Id { get; set; }
    public Guid OutboxMessageId { get; set; }
    public Guid IdempotencyKey { get; set; }
    public string EventType { get; set; } = null!;
    public string EventData { get; set; } = null!;
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTime FailedAt { get; set; }
    public DateTime? RequeuedAt { get; set; }
}
