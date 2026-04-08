namespace ECommerce.Infrastructure.Integration;

public sealed class OutboxDispatcherOptions
{
    public int MaxRetryAttempts { get; set; } = 5;

    public int BaseRetryDelaySeconds { get; set; } = 5;

    public int MaxRetryDelaySeconds { get; set; } = 300;

    public int BatchSize { get; set; } = 100;
}
