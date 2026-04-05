using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using ECommerce.Inventory.Domain.Interfaces;
using ECommerce.Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Infrastructure.Persistence.Repositories;

public class InventoryItemRepository(InventoryDbContext _db) : IInventoryItemRepository
{
    public async Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.InventoryItems
            .Include("_logEntries")
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        => await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == productId, ct);

    public async Task<InventoryItem?> GetByProductIdWithLogsAsync(Guid productId, CancellationToken ct = default)
        => await _db.InventoryItems
            .Include("_logEntries")
            .FirstOrDefaultAsync(i => i.ProductId == productId, ct);

    public async Task<List<InventoryItem>> GetAllAsync(CancellationToken ct = default)
        => await _db.InventoryItems
            .Include("_logEntries")
            .ToListAsync(ct);

    public async Task<List<InventoryItem>> GetLowStockAsync(int? thresholdOverride = null, CancellationToken ct = default)
        => await _db.InventoryItems
            .Include("_logEntries")
            .Where(i => i.Stock.Quantity <= (thresholdOverride ?? i.LowStockThreshold))
            .ToListAsync(ct);

    public async Task AddAsync(InventoryItem item, CancellationToken ct = default)
        => await _db.InventoryItems.AddAsync(item, ct);

    public async Task UpdateAsync(InventoryItem item, CancellationToken ct = default)
    {
        _db.InventoryItems.Update(item);
        await Task.CompletedTask;
    }
}
