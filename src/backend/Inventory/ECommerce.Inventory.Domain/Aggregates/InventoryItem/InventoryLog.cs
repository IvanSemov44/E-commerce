using ECommerce.SharedKernel.Domain;

namespace ECommerce.Inventory.Domain.Aggregates.InventoryItem;

public sealed class InventoryLog : Entity
{
    public Guid InventoryItemId { get; private set; }
    public int Delta { get; private set; }
    public string Reason { get; private set; } = null!;
    public int StockAfter { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private InventoryLog() { }

    internal static InventoryLog Create(Guid inventoryItemId, int delta, string reason, int stockAfter)
        => new()
        {
            Id = Guid.NewGuid(),
            InventoryItemId = inventoryItemId,
            Delta = delta,
            Reason = reason,
            StockAfter = stockAfter,
            OccurredAt = DateTime.UtcNow,
        };
}