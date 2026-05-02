namespace ECommerce.Ordering.Infrastructure.Integration;

public sealed class OrderingOutboxDispatcherOptions
{
    public int BatchSize { get; set; } = 20;
    public int MaxRetryAttempts { get; set; } = 5;
    public int BaseRetryDelaySeconds { get; set; } = 5;
    public int MaxRetryDelaySeconds { get; set; } = 300;
}
