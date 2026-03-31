# Phase 3, Step 6: Inventory Handler Unit Tests

**Prerequisite**: Step 2 (`ECommerce.Inventory.Application`) is complete and builds. `ECommerce.Inventory.Tests` project from step 5 exists.

Write these tests AFTER delivering the Application project. They test command/query handler orchestration using fake repository and service implementations — no EF, no HTTP, no database.

---

## Task: Add Handler Tests to ECommerce.Inventory.Tests

Files go in `Inventory/ECommerce.Inventory.Tests/Application/`.

Start by creating the shared test fakes — both test files below reference them.

---

### File: `Inventory/ECommerce.Inventory.Tests/Application/Fakes.cs`

```csharp
using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using ECommerce.Inventory.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Inventory.Tests.Application;

/// <summary>
/// Shared fakes for all Application handler tests.
/// </summary>
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
    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
```

---

---

### File: `Inventory/ECommerce.Inventory.Tests/Application/CommandHandlerTests.cs`

```csharp
using ECommerce.Inventory.Application.Commands.IncreaseStock;
using ECommerce.Inventory.Application.Commands.ReduceStock;
using ECommerce.Inventory.Application.Commands.AdjustStock;
using ECommerce.Inventory.Application.Errors;
using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using ECommerce.Inventory.Domain.Errors;
using ECommerce.Inventory.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.Inventory.Tests.Application;

[TestClass]
public class CommandHandlerTests
{
    // Helper to create a seeded InventoryItem
    private static InventoryItem MakeItem(Guid productId, int quantity = 50, int threshold = 10)
        => InventoryItem.Create(productId, quantity, threshold).GetDataOrThrow();

    // ── IncreaseStockCommandHandler ────────────────────────────────────────────

    [TestMethod]
    public async Task IncreaseStock_ExistingItem_IncreasesQuantityAndSaves()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        var uow  = new FakeUnitOfWork();
        repo.Store.Add(MakeItem(productId, quantity: 30));

        var handler = new IncreaseStockCommandHandler(repo, uow);
        var result  = await handler.Handle(new IncreaseStockCommand(productId, 20, "restock"), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(50, result.GetDataOrThrow().NewQuantity);
        Assert.AreEqual(1, uow.SaveCount);
    }

    [TestMethod]
    public async Task IncreaseStock_UnknownProduct_ReturnsInventoryItemNotFound()
    {
        var repo = new FakeInventoryItemRepository();  // empty
        var uow  = new FakeUnitOfWork();

        var handler = new IncreaseStockCommandHandler(repo, uow);
        var result  = await handler.Handle(new IncreaseStockCommand(Guid.NewGuid(), 10, "restock"), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryApplicationErrors.InventoryItemNotFound.Code, result.GetErrorOrThrow().Code);
        Assert.AreEqual(0, uow.SaveCount);
    }

    [TestMethod]
    public async Task IncreaseStock_ZeroAmount_ReturnsIncreaseAmountInvalid()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        repo.Store.Add(MakeItem(productId));
        var uow = new FakeUnitOfWork();

        var handler = new IncreaseStockCommandHandler(repo, uow);
        var result  = await handler.Handle(new IncreaseStockCommand(productId, 0, "restock"), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.IncreaseAmountInvalid.Code, result.GetErrorOrThrow().Code);
        Assert.AreEqual(0, uow.SaveCount);
    }

    // ── ReduceStockCommandHandler ──────────────────────────────────────────────

    [TestMethod]
    public async Task ReduceStock_ExistingItem_ReducesQuantityAndSaves()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        var uow  = new FakeUnitOfWork();
        repo.Store.Add(MakeItem(productId, quantity: 50));

        var handler = new ReduceStockCommandHandler(repo, uow);
        var result  = await handler.Handle(new ReduceStockCommand(productId, 20, "sale"), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(30, result.GetDataOrThrow().NewQuantity);
        Assert.AreEqual(1, uow.SaveCount);
    }

    [TestMethod]
    public async Task ReduceStock_InsufficientStock_ReturnsFailureWithoutSaving()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        var uow  = new FakeUnitOfWork();
        repo.Store.Add(MakeItem(productId, quantity: 5));

        var handler = new ReduceStockCommandHandler(repo, uow);
        var result  = await handler.Handle(new ReduceStockCommand(productId, 10, "sale"), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.InsufficientStock.Code, result.GetErrorOrThrow().Code);
        Assert.AreEqual(0, uow.SaveCount);
    }

    [TestMethod]
    public async Task ReduceStock_UnknownProduct_ReturnsInventoryItemNotFound()
    {
        var repo = new FakeInventoryItemRepository();
        var uow  = new FakeUnitOfWork();

        var handler = new ReduceStockCommandHandler(repo, uow);
        var result  = await handler.Handle(new ReduceStockCommand(Guid.NewGuid(), 5, "sale"), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryApplicationErrors.InventoryItemNotFound.Code, result.GetErrorOrThrow().Code);
    }

    // ── AdjustStockCommandHandler ──────────────────────────────────────────────

    [TestMethod]
    public async Task AdjustStock_ExistingItem_SetsQuantityToNewValue()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        var uow  = new FakeUnitOfWork();
        repo.Store.Add(MakeItem(productId, quantity: 30));

        var handler = new AdjustStockCommandHandler(repo, uow);
        var result  = await handler.Handle(new AdjustStockCommand(productId, 100, "inventory_count"), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(100, result.GetDataOrThrow().NewQuantity);
        Assert.AreEqual(70, result.GetDataOrThrow().QuantityChanged);  // 100 - 30 = +70
        Assert.AreEqual(1, uow.SaveCount);
    }

    [TestMethod]
    public async Task AdjustStock_ToZero_Succeeds()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        var uow  = new FakeUnitOfWork();
        repo.Store.Add(MakeItem(productId, quantity: 30));

        var handler = new AdjustStockCommandHandler(repo, uow);
        var result  = await handler.Handle(new AdjustStockCommand(productId, 0, "write-off"), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(0, result.GetDataOrThrow().NewQuantity);
    }

    [TestMethod]
    public async Task AdjustStock_NegativeQuantity_ReturnsFailureWithoutSaving()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        var uow  = new FakeUnitOfWork();
        repo.Store.Add(MakeItem(productId, quantity: 30));

        var handler = new AdjustStockCommandHandler(repo, uow);
        var result  = await handler.Handle(new AdjustStockCommand(productId, -1, "error"), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(0, uow.SaveCount);
    }
}
```

---

### File: `Inventory/ECommerce.Inventory.Tests/Application/QueryHandlerTests.cs`

```csharp
using ECommerce.Inventory.Application.Errors;
using ECommerce.Inventory.Application.Queries.GetInventory;
using ECommerce.Inventory.Application.Queries.GetInventoryByProductId;
using ECommerce.Inventory.Application.Queries.GetInventoryHistory;
using ECommerce.Inventory.Application.Queries.GetLowStockItems;
using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using ECommerce.Inventory.Domain.Interfaces;

namespace ECommerce.Inventory.Tests.Application;

[TestClass]
public class QueryHandlerTests
{
    // Uses FakeInventoryItemRepository from Fakes.cs

    private static InventoryItem MakeItem(Guid productId, int quantity = 50, int threshold = 10)
        => InventoryItem.Create(productId, quantity, threshold).GetDataOrThrow();

    // ── GetInventoryQueryHandler ───────────────────────────────────────────────

    [TestMethod]
    public async Task GetInventory_EmptyStore_ReturnsEmptyList()
    {
        var repo    = new FakeInventoryItemRepository();
        var handler = new GetInventoryQueryHandler(repo);

        var result = await handler.Handle(new GetInventoryQuery(), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(0, result.GetDataOrThrow().Count);
    }

    [TestMethod]
    public async Task GetInventory_MultipleItems_ReturnsAll()
    {
        var repo = new FakeInventoryItemRepository();
        repo.Store.Add(MakeItem(Guid.NewGuid(), 100, 10));
        repo.Store.Add(MakeItem(Guid.NewGuid(), 5, 10));
        var handler = new GetInventoryQueryHandler(repo);

        var result = await handler.Handle(new GetInventoryQuery(), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(2, result.GetDataOrThrow().Count);
    }

    [TestMethod]
    public async Task GetInventory_LowStockOnly_ReturnsOnlyLowStockItems()
    {
        var repo = new FakeInventoryItemRepository();
        repo.Store.Add(MakeItem(Guid.NewGuid(), 100, 10));  // not low stock
        repo.Store.Add(MakeItem(Guid.NewGuid(), 5, 10));    // low stock
        var handler = new GetInventoryQueryHandler(repo);

        var result = await handler.Handle(new GetInventoryQuery(LowStockOnly: true), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.GetDataOrThrow().Count);
        Assert.IsTrue(result.GetDataOrThrow()[0].IsLowStock);
    }

    [TestMethod]
    public async Task GetInventory_DtoFields_AreCorrectlyMapped()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        repo.Store.Add(MakeItem(productId, quantity: 8, threshold: 10));
        var handler = new GetInventoryQueryHandler(repo);

        var result = await handler.Handle(new GetInventoryQuery(), default);
        var dto    = result.GetDataOrThrow()[0];

        Assert.AreEqual(productId, dto.ProductId);
        Assert.AreEqual(8, dto.Quantity);
        Assert.AreEqual(10, dto.LowStockThreshold);
        Assert.IsTrue(dto.IsLowStock);
        Assert.IsFalse(dto.IsOutOfStock);
    }

    // ── GetInventoryByProductIdQueryHandler ───────────────────────────────────

    [TestMethod]
    public async Task GetInventoryByProductId_ExistingProduct_ReturnsDto()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        repo.Store.Add(MakeItem(productId, quantity: 50));
        var handler = new GetInventoryByProductIdQueryHandler(repo);

        var result = await handler.Handle(new GetInventoryByProductIdQuery(productId), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(productId, result.GetDataOrThrow().ProductId);
        Assert.AreEqual(50, result.GetDataOrThrow().Quantity);
    }

    [TestMethod]
    public async Task GetInventoryByProductId_UnknownProduct_ReturnsNotFound()
    {
        var repo    = new FakeInventoryItemRepository();
        var handler = new GetInventoryByProductIdQueryHandler(repo);

        var result = await handler.Handle(new GetInventoryByProductIdQuery(Guid.NewGuid()), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryApplicationErrors.InventoryItemNotFound.Code, result.GetErrorOrThrow().Code);
    }

    // ── GetInventoryHistoryQueryHandler ──────────────────────────────────────

    [TestMethod]
    public async Task GetInventoryHistory_ExistingProduct_ReturnsLogEntries()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        var item = MakeItem(productId, quantity: 50);
        item.Reduce(10, "sale");
        item.Increase(5, "restock");
        repo.Store.Add(item);
        var handler = new GetInventoryHistoryQueryHandler(repo);

        var result = await handler.Handle(new GetInventoryHistoryQuery(productId), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(2, result.GetDataOrThrow().Count);
    }

    [TestMethod]
    public async Task GetInventoryHistory_UnknownProduct_ReturnsNotFound()
    {
        var repo    = new FakeInventoryItemRepository();
        var handler = new GetInventoryHistoryQueryHandler(repo);

        var result = await handler.Handle(new GetInventoryHistoryQuery(Guid.NewGuid()), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryApplicationErrors.InventoryItemNotFound.Code, result.GetErrorOrThrow().Code);
    }

    // ── GetLowStockItemsQueryHandler ──────────────────────────────────────────

    [TestMethod]
    public async Task GetLowStockItems_WithThresholdOverride_ReturnsItemsBelowOverride()
    {
        var repo = new FakeInventoryItemRepository();
        repo.Store.Add(MakeItem(Guid.NewGuid(), quantity: 15, threshold: 10));  // not low by item threshold
        repo.Store.Add(MakeItem(Guid.NewGuid(), quantity: 5, threshold: 10));   // low by item threshold
        var handler = new GetLowStockItemsQueryHandler(repo);

        // Query with override threshold of 20 — all items are below 20
        var result = await handler.Handle(new GetLowStockItemsQuery(ThresholdOverride: 20), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(2, result.GetDataOrThrow().Count);
    }
}
```

---

### File: `Inventory/ECommerce.Inventory.Tests/Application/EventHandlerTests.cs`

```csharp
using ECommerce.Inventory.Application.EventHandlers;
using ECommerce.Inventory.Application.Interfaces;
using ECommerce.Inventory.Domain.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.Inventory.Tests.Application;

[TestClass]
public class EventHandlerTests
{
    // ── Fakes ─────────────────────────────────────────────────────────────────

    sealed class FakeEmailService : IEmailService
    {
        public List<(Guid ProductId, int CurrentStock, int Threshold)> Calls = new();
        public bool ShouldThrow { get; set; }

        public Task SendLowStockAlertAsync(Guid productId, int currentStock, int threshold, CancellationToken ct)
        {
            if (ShouldThrow) throw new InvalidOperationException("Email service is down");
            Calls.Add((productId, currentStock, threshold));
            return Task.CompletedTask;
        }
    }

    // ── SendLowStockAlertOnLowStockDetectedHandler ────────────────────────────

    [TestMethod]
    public async Task Handle_LowStockDetectedEvent_CallsEmailService()
    {
        var email   = new FakeEmailService();
        var handler = new SendLowStockAlertOnLowStockDetectedHandler(
            email, NullLogger<SendLowStockAlertOnLowStockDetectedHandler>.Instance);

        var productId = Guid.NewGuid();
        await handler.Handle(new LowStockDetectedEvent(productId, 5, 10), default);

        Assert.AreEqual(1, email.Calls.Count);
        Assert.AreEqual(productId, email.Calls[0].ProductId);
        Assert.AreEqual(5, email.Calls[0].CurrentStock);
        Assert.AreEqual(10, email.Calls[0].Threshold);
    }

    [TestMethod]
    public async Task Handle_EmailServiceThrows_DoesNotRethrow()
    {
        // Rule 17: event handlers catch exceptions — they must never throw to callers
        var email = new FakeEmailService { ShouldThrow = true };
        var handler = new SendLowStockAlertOnLowStockDetectedHandler(
            email, NullLogger<SendLowStockAlertOnLowStockDetectedHandler>.Instance);

        // Must NOT throw
        await handler.Handle(new LowStockDetectedEvent(Guid.NewGuid(), 2, 10), default);
    }

    [TestMethod]
    public async Task Handle_EmailServiceThrows_DoesNotCallSaveOrOtherSideEffects()
    {
        // After exception, handler exits cleanly — no partial state
        var email = new FakeEmailService { ShouldThrow = true };
        var handler = new SendLowStockAlertOnLowStockDetectedHandler(
            email, NullLogger<SendLowStockAlertOnLowStockDetectedHandler>.Instance);

        var exception = await Record.ExceptionAsync(() =>
            handler.Handle(new LowStockDetectedEvent(Guid.NewGuid(), 2, 10), default));

        Assert.IsNull(exception, "Handler must not propagate exceptions");
    }
}

// Helper for async exception recording
file static class Record
{
    public static async Task<Exception?> ExceptionAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
```

### 3. Run handler tests

```bash
cd src/backend
dotnet test Inventory/ECommerce.Inventory.Tests/ECommerce.Inventory.Tests.csproj \
    --filter "FullyQualifiedName~CommandHandlerTests|FullyQualifiedName~QueryHandlerTests|FullyQualifiedName~EventHandlerTests"
```

---

## Acceptance Criteria

- [ ] `Fakes.cs` created with `FakeInventoryItemRepository` and `FakeUnitOfWork` shared across test files
- [ ] `CommandHandlerTests.cs` created — all tests pass
  - Covers: `IncreaseStockCommand` (success, unknown product, zero amount), `ReduceStockCommand` (success, insufficient, unknown), `AdjustStockCommand` (success, to zero, negative)
- [ ] `QueryHandlerTests.cs` created — all tests pass
  - Covers: `GetInventoryQuery` (empty, multiple, low stock filter, DTO mapping), `GetInventoryByProductIdQuery` (found, not found), `GetInventoryHistoryQuery` (entries returned, unknown product), `GetLowStockItemsQuery` (threshold override)
- [ ] `EventHandlerTests.cs` created — all tests pass
  - Covers: `SendLowStockAlertOnLowStockDetectedHandler` calls email service, catches exception without rethrowing (Rule 17)
- [ ] All tests are fast (no I/O, no database, no real email)
- [ ] `SaveChangesAsync` called exactly once on success, never on failure
