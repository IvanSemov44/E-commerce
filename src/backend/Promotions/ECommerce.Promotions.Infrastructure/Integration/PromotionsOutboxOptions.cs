namespace ECommerce.Promotions.Infrastructure.Integration;

/// <summary>
/// Configuration options for the Promotions outbox dispatcher.
/// </summary>
public sealed class PromotionsOutboxOptions
{
    public int MaxRetryAttempts { get; set; } = 5;
    public int BaseRetryDelaySeconds { get; set; } = 5;
    public int MaxRetryDelaySeconds { get; set; } = 300;
    public int BatchSize { get; set; } = 100;
}
