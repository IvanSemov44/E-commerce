using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using ECommerce.Inventory.Domain.Interfaces;
using ECommerce.Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Infrastructure.Persistence.Repositories;

public class InventoryItemRepository(InventoryDbContext _db) : IInventoryItemRepository
{
    public Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.InventoryItems
            .Include("_logEntries")
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        => _db.InventoryItems
            .Include("_logEntries")
            .FirstOrDefaultAsync(i => i.ProductId == productId, ct);

    public Task<InventoryItem?> GetByProductIdWithLogsAsync(Guid productId, CancellationToken ct = default)
        => _db.InventoryItems
            .Include("_logEntries")
            .FirstOrDefaultAsync(i => i.ProductId == productId, ct);

    public Task<List<InventoryItem>> GetAllAsync(CancellationToken ct = default)
        => _db.InventoryItems
            .Include("_logEntries")
            .ToListAsync(ct);

    public Task<List<InventoryItem>> GetLowStockAsync(int? thresholdOverride = null, CancellationToken ct = default)
        => _db.InventoryItems
            .Include("_logEntries")
            .Where(i => i.Stock.Quantity <= (thresholdOverride ?? i.LowStockThreshold))
            .ToListAsync(ct);

    public Task AddAsync(InventoryItem item, CancellationToken ct = default)
        => _db.InventoryItems.AddAsync(item, ct).AsTask();

    public Task UpdateAsync(InventoryItem item, CancellationToken ct = default)
    {
        _db.InventoryItems.Update(item);
        return Task.CompletedTask;
    }
}
