namespace ECommerce.Reviews.Infrastructure.Integration;

public sealed class ReviewsOutboxDispatcherOptions
{
    public int MaxRetryAttempts { get; set; } = 5;

    public int BaseRetryDelaySeconds { get; set; } = 5;

    public int MaxRetryDelaySeconds { get; set; } = 300;

    public int BatchSize { get; set; } = 100;
}
