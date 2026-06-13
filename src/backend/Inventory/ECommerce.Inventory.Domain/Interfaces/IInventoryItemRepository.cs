using ECommerce.Inventory.Domain.Aggregates.InventoryItem;

namespace ECommerce.Inventory.Domain.Interfaces;

public interface IInventoryItemRepository
{
    Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<InventoryItem?> GetByProductIdWithLogsAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<List<InventoryItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<InventoryItem>> GetLowStockAsync(int? thresholdOverride = null, CancellationToken cancellationToken = default);
    Task<List<InventoryItem>> GetByProductIdsAsync(IList<Guid> productIds, CancellationToken cancellationToken = default);
    Task<(List<InventoryItem> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search, bool lowStockOnly, CancellationToken cancellationToken = default);
    Task<(List<InventoryItem> Items, int TotalCount)> GetLowStockPagedAsync(int page, int pageSize, int? thresholdOverride, CancellationToken cancellationToken = default);
    Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default);
    Task UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default);
}
