namespace ECommerce.Shopping.Infrastructure.Persistence;

public class InboxMessage
{
    public Guid Id { get; set; }

    public Guid IdempotencyKey { get; set; }

    public string EventType { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public int AttemptCount { get; set; }

    public string? LastError { get; set; }
}
