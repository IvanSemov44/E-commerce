namespace ECommerce.Inventory.Infrastructure.Integration;

public sealed class InventoryOutboxDispatcherOptions
{
    public int BatchSize { get; set; } = 50;
    public int MaxRetryAttempts { get; set; } = 5;
    public int BaseRetryDelaySeconds { get; set; } = 5;
    public int MaxRetryDelaySeconds { get; set; } = 300;
}
