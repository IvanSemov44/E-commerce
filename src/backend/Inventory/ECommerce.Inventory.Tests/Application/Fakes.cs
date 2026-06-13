using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using ECommerce.Inventory.Domain.Interfaces;

namespace ECommerce.Inventory.Tests.Application;

sealed class FakeInventoryItemRepository : IInventoryItemRepository
{
    public List<InventoryItem> Store = new();

    public Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Store.FirstOrDefault(i => i.Id == id));

    public Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        => Task.FromResult(Store.FirstOrDefault(i => i.ProductId == productId));

    public Task<InventoryItem?> GetByProductIdWithLogsAsync(Guid productId, CancellationToken ct = default)
        => Task.FromResult(Store.FirstOrDefault(i => i.ProductId == productId));

    public Task<List<InventoryItem>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(Store.ToList());

    public Task<List<InventoryItem>> GetLowStockAsync(int? thresholdOverride = null, CancellationToken ct = default)
        => Task.FromResult(Store.Where(i =>
            i.Stock.Quantity <= (thresholdOverride ?? i.LowStockThreshold)).ToList());

    public Task<List<InventoryItem>> GetByProductIdsAsync(IList<Guid> productIds, CancellationToken ct = default)
        => Task.FromResult(Store.Where(i => productIds.Contains(i.ProductId)).ToList());

    public Task<(List<InventoryItem> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, bool lowStockOnly, CancellationToken ct = default)
    {
        var query = lowStockOnly
            ? Store.Where(i => i.Stock.Quantity <= i.LowStockThreshold)
            : Store.AsEnumerable();

        var total = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult((items, total));
    }

    public Task<(List<InventoryItem> Items, int TotalCount)> GetLowStockPagedAsync(
        int page, int pageSize, int? thresholdOverride, CancellationToken ct = default)
    {
        var query = Store.Where(i =>
            i.Stock.Quantity <= (thresholdOverride ?? i.LowStockThreshold));

        var total = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult((items, total));
    }

    public Task AddAsync(InventoryItem item, CancellationToken ct = default)
    {
        Store.Add(item);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(InventoryItem item, CancellationToken ct = default)
    {
        var index = Store.FindIndex(i => i.Id == item.Id);
        if (index >= 0)
            Store[index] = item;
        else
            Store.Add(item);

        return Task.CompletedTask;
    }
}
