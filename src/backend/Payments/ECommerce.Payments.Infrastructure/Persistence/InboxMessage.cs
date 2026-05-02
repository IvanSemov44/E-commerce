namespace ECommerce.Payments.Infrastructure.Persistence;

public sealed class InboxMessage
{
    public Guid Id { get; set; }
    public Guid IdempotencyKey { get; set; }
    public string EventType { get; set; } = null!;
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}
