namespace ECommerce.Payments.Infrastructure.Persistence;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid IdempotencyKey { get; set; }
    public string EventType { get; set; } = null!;
    public string EventData { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? DeadLetteredAt { get; set; }
    public bool IsDeadLettered { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}
