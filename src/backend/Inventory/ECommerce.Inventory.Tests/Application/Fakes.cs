using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using ECommerce.Inventory.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Inventory.Tests.Application;

sealed class FakeInventoryItemRepository : IInventoryItemRepository
{
    public List<InventoryItem> Store = new();

    public Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Store.FirstOrDefault(i => i.Id == id));

    public Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        => Task.FromResult(Store.FirstOrDefault(i => i.ProductId == productId));

    public Task<List<InventoryItem>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(Store.ToList());

    public Task<List<InventoryItem>> GetLowStockAsync(int? thresholdOverride = null, CancellationToken ct = default)
        => Task.FromResult(Store.Where(i =>
            i.Stock.Quantity <= (thresholdOverride ?? i.LowStockThreshold)).ToList());

    public Task AddAsync(InventoryItem item, CancellationToken ct = default)
    {
        Store.Add(item);
        return Task.CompletedTask;
    }
}

sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveCount++;
        return Task.FromResult(SaveCount);
    }

    public Task BeginTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task CommitTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task RollbackTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
    public bool HasActiveTransaction => false;
    public void Dispose() { }
}