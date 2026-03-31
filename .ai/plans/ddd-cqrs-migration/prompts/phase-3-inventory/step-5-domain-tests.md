# Phase 3, Step 5: Inventory Domain Unit Tests

**Prerequisite**: Step 1 (`ECommerce.Inventory.Domain`) is complete and builds.

Write these tests AFTER delivering the domain project. They test the `StockLevel` value object and `InventoryItem` aggregate in isolation — no EF, no HTTP, no database.

---

## Task: Create ECommerce.Inventory.Tests Project

### 1. Create the test project

```bash
cd src/backend
dotnet new mstest -n ECommerce.Inventory.Tests -f net10.0 -o Inventory/ECommerce.Inventory.Tests
dotnet sln ../../ECommerce.sln add Inventory/ECommerce.Inventory.Tests/ECommerce.Inventory.Tests.csproj

dotnet add Inventory/ECommerce.Inventory.Tests/ECommerce.Inventory.Tests.csproj \
    reference Inventory/ECommerce.Inventory.Domain/ECommerce.Inventory.Domain.csproj
dotnet add Inventory/ECommerce.Inventory.Tests/ECommerce.Inventory.Tests.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj

# Delete auto-generated test file
rm Inventory/ECommerce.Inventory.Tests/UnitTest1.cs
```

### 2. Create domain unit tests

**File: `Inventory/ECommerce.Inventory.Tests/Domain/StockLevelTests.cs`**

```csharp
using ECommerce.Inventory.Domain.Errors;
using ECommerce.Inventory.Domain.ValueObjects;

namespace ECommerce.Inventory.Tests.Domain;

[TestClass]
public class StockLevelTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Create_ZeroQuantity_Succeeds()
    {
        var result = StockLevel.Create(0);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(0, result.GetDataOrThrow().Quantity);
    }

    [TestMethod]
    public void Create_PositiveQuantity_Succeeds()
    {
        var result = StockLevel.Create(100);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(100, result.GetDataOrThrow().Quantity);
    }

    [TestMethod]
    public void Create_NegativeQuantity_ReturnsFailure()
    {
        var result = StockLevel.Create(-1);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.StockNegative.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Zero_Property_HasZeroQuantity()
    {
        Assert.AreEqual(0, StockLevel.Zero.Quantity);
    }

    // ── Reduce ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Reduce_ValidAmount_ReturnsNewInstanceWithReducedQuantity()
    {
        var stock = StockLevel.Create(50).GetDataOrThrow();

        var result = stock.Reduce(20);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(30, result.GetDataOrThrow().Quantity);
        // Original unchanged (immutable)
        Assert.AreEqual(50, stock.Quantity);
    }

    [TestMethod]
    public void Reduce_ExactStock_ReturnsZero()
    {
        var stock = StockLevel.Create(10).GetDataOrThrow();

        var result = stock.Reduce(10);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(0, result.GetDataOrThrow().Quantity);
    }

    [TestMethod]
    public void Reduce_MoreThanStock_ReturnsInsufficientStock()
    {
        var stock = StockLevel.Create(5).GetDataOrThrow();

        var result = stock.Reduce(6);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.InsufficientStock.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Reduce_ZeroAmount_ReturnsFailure()
    {
        var stock = StockLevel.Create(50).GetDataOrThrow();

        var result = stock.Reduce(0);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.ReduceAmountInvalid.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Reduce_NegativeAmount_ReturnsFailure()
    {
        var stock = StockLevel.Create(50).GetDataOrThrow();

        var result = stock.Reduce(-5);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.ReduceAmountInvalid.Code, result.GetErrorOrThrow().Code);
    }

    // ── Increase ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void Increase_ValidAmount_ReturnsNewInstanceWithIncreasedQuantity()
    {
        var stock = StockLevel.Create(30).GetDataOrThrow();

        var result = stock.Increase(20);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(50, result.GetDataOrThrow().Quantity);
        // Original unchanged (immutable)
        Assert.AreEqual(30, stock.Quantity);
    }

    [TestMethod]
    public void Increase_ZeroAmount_ReturnsFailure()
    {
        var stock = StockLevel.Create(30).GetDataOrThrow();

        var result = stock.Increase(0);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.IncreaseAmountInvalid.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Increase_NegativeAmount_ReturnsFailure()
    {
        var stock = StockLevel.Create(30).GetDataOrThrow();

        var result = stock.Increase(-10);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.IncreaseAmountInvalid.Code, result.GetErrorOrThrow().Code);
    }

    // ── Equality ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void TwoStockLevels_SameQuantity_AreEqual()
    {
        var a = StockLevel.Create(42).GetDataOrThrow();
        var b = StockLevel.Create(42).GetDataOrThrow();

        Assert.AreEqual(a, b);
    }

    [TestMethod]
    public void TwoStockLevels_DifferentQuantity_AreNotEqual()
    {
        var a = StockLevel.Create(42).GetDataOrThrow();
        var b = StockLevel.Create(43).GetDataOrThrow();

        Assert.AreNotEqual(a, b);
    }
}
```

---

**File: `Inventory/ECommerce.Inventory.Tests/Domain/InventoryItemTests.cs`**

```csharp
using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using ECommerce.Inventory.Domain.Errors;
using ECommerce.Inventory.Domain.Events;

namespace ECommerce.Inventory.Tests.Domain;

[TestClass]
public class InventoryItemTests
{
    private static readonly Guid TestProductId = Guid.NewGuid();

    // ── Create ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Create_ValidInputs_Succeeds()
    {
        var result = InventoryItem.Create(TestProductId, 100, 10);

        Assert.IsTrue(result.IsSuccess);
        var item = result.GetDataOrThrow();
        Assert.AreEqual(TestProductId, item.ProductId);
        Assert.AreEqual(100, item.Stock.Quantity);
        Assert.AreEqual(10, item.LowStockThreshold);
        Assert.IsTrue(item.TrackInventory);
    }

    [TestMethod]
    public void Create_ZeroInitialQuantity_Succeeds()
    {
        var result = InventoryItem.Create(TestProductId, 0, 5);
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void Create_NegativeInitialQuantity_ReturnsFailure()
    {
        var result = InventoryItem.Create(TestProductId, -1, 5);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.StockNegative.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Create_NegativeThreshold_ReturnsFailure()
    {
        var result = InventoryItem.Create(TestProductId, 50, -1);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.ThresholdNegative.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Create_ZeroThreshold_Succeeds()
    {
        var result = InventoryItem.Create(TestProductId, 10, 0);
        Assert.IsTrue(result.IsSuccess);
    }

    // ── Reduce ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Reduce_ValidAmount_UpdatesStockAndAddsLog()
    {
        var item = InventoryItem.Create(TestProductId, 50, 10).GetDataOrThrow();

        var result = item.Reduce(20, "sale");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(30, item.Stock.Quantity);
        Assert.AreEqual(1, item.Log.Count);
        Assert.AreEqual(-20, item.Log.First().Delta);
        Assert.AreEqual("sale", item.Log.First().Reason);
    }

    [TestMethod]
    public void Reduce_InsufficientStock_ReturnsFailure()
    {
        var item = InventoryItem.Create(TestProductId, 5, 10).GetDataOrThrow();

        var result = item.Reduce(10, "sale");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.InsufficientStock.Code, result.GetErrorOrThrow().Code);
        // Stock unchanged
        Assert.AreEqual(5, item.Stock.Quantity);
    }

    [TestMethod]
    public void Reduce_RaisesStockReducedEvent()
    {
        var item = InventoryItem.Create(TestProductId, 50, 10).GetDataOrThrow();

        item.Reduce(20, "sale");

        var events = item.DomainEvents.OfType<StockReducedEvent>().ToList();
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(20, events[0].QuantityReduced);
        Assert.AreEqual(30, events[0].NewQuantity);
        Assert.AreEqual("sale", events[0].Reason);
    }

    // ── LowStockDetectedEvent threshold crossing ──────────────────────────────

    [TestMethod]
    public void Reduce_CrossingThresholdFromAbove_RaisesLowStockDetectedEvent()
    {
        // Stock starts above threshold (20 > 10), reduce to below (5 <= 10)
        var item = InventoryItem.Create(TestProductId, 20, 10).GetDataOrThrow();

        item.Reduce(15, "sale");  // 20 → 5, crosses threshold

        var events = item.DomainEvents.OfType<LowStockDetectedEvent>().ToList();
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(5, events[0].CurrentStock);
        Assert.AreEqual(10, events[0].Threshold);
    }

    [TestMethod]
    public void Reduce_AlreadyBelowThreshold_DoesNotRaiseLowStockDetectedEvent()
    {
        // Stock already below threshold — no threshold crossing
        var item = InventoryItem.Create(TestProductId, 5, 10).GetDataOrThrow();
        item.ClearDomainEvents();  // clear any events from Create

        item.Reduce(2, "sale");  // 5 → 3, already below threshold

        var events = item.DomainEvents.OfType<LowStockDetectedEvent>().ToList();
        Assert.AreEqual(0, events.Count, "LowStockDetectedEvent must NOT fire when stock was already below threshold");
    }

    [TestMethod]
    public void Reduce_StockExactlyAtThreshold_DoesNotRaiseLowStockDetectedEvent()
    {
        // Stock exactly at threshold (10 == 10) — already at threshold, no crossing from above
        var item = InventoryItem.Create(TestProductId, 10, 10).GetDataOrThrow();
        item.ClearDomainEvents();

        item.Reduce(1, "sale");  // 10 → 9, was AT threshold not above

        var events = item.DomainEvents.OfType<LowStockDetectedEvent>().ToList();
        Assert.AreEqual(0, events.Count, "LowStockDetectedEvent must NOT fire when previous stock was already <= threshold");
    }

    [TestMethod]
    public void Reduce_ToExactlyThreshold_RaisesLowStockDetectedEvent()
    {
        // Stock starts above threshold (15 > 10), reduce to exactly threshold (10 <= 10)
        var item = InventoryItem.Create(TestProductId, 15, 10).GetDataOrThrow();
        item.ClearDomainEvents();

        item.Reduce(5, "sale");  // 15 → 10, crosses to threshold

        var events = item.DomainEvents.OfType<LowStockDetectedEvent>().ToList();
        Assert.AreEqual(1, events.Count);
    }

    // ── Increase ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void Increase_ValidAmount_UpdatesStockAndAddsLog()
    {
        var item = InventoryItem.Create(TestProductId, 30, 10).GetDataOrThrow();

        var result = item.Increase(20, "restock");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(50, item.Stock.Quantity);
        Assert.AreEqual(1, item.Log.Count);
        Assert.AreEqual(20, item.Log.First().Delta);
    }

    [TestMethod]
    public void Increase_RaisesStockReplenishedEvent()
    {
        var item = InventoryItem.Create(TestProductId, 30, 10).GetDataOrThrow();

        item.Increase(20, "restock");

        var events = item.DomainEvents.OfType<StockReplenishedEvent>().ToList();
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(20, events[0].QuantityAdded);
        Assert.AreEqual(50, events[0].NewQuantity);
    }

    [TestMethod]
    public void Increase_ZeroAmount_ReturnsFailure()
    {
        var item = InventoryItem.Create(TestProductId, 30, 10).GetDataOrThrow();

        var result = item.Increase(0, "restock");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.IncreaseAmountInvalid.Code, result.GetErrorOrThrow().Code);
    }

    // ── Adjust ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Adjust_ValidQuantity_SetsStockAndAddsLog()
    {
        var item = InventoryItem.Create(TestProductId, 30, 10).GetDataOrThrow();

        var result = item.Adjust(100, "inventory_count");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(100, item.Stock.Quantity);
        Assert.AreEqual(1, item.Log.Count);
        Assert.AreEqual(70, item.Log.First().Delta);  // 100 - 30 = +70
    }

    [TestMethod]
    public void Adjust_ToZero_Succeeds()
    {
        var item = InventoryItem.Create(TestProductId, 30, 10).GetDataOrThrow();

        var result = item.Adjust(0, "write-off");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(0, item.Stock.Quantity);
    }

    [TestMethod]
    public void Adjust_NegativeQuantity_ReturnsFailure()
    {
        var item = InventoryItem.Create(TestProductId, 30, 10).GetDataOrThrow();

        var result = item.Adjust(-5, "error");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.StockNegative.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Adjust_DoesNotRaiseDomainEvents()
    {
        // Adjust is an admin override — does not raise business events
        var item = InventoryItem.Create(TestProductId, 30, 10).GetDataOrThrow();
        item.ClearDomainEvents();

        item.Adjust(100, "correction");

        Assert.AreEqual(0, item.DomainEvents.Count,
            "Adjust must not raise domain events — it is an admin override");
    }

    // ── Log entries ───────────────────────────────────────────────────────────

    [TestMethod]
    public void MultipleOperations_LogEntriesAccumulate()
    {
        var item = InventoryItem.Create(TestProductId, 100, 10).GetDataOrThrow();

        item.Reduce(20, "sale");
        item.Increase(50, "restock");
        item.Adjust(80, "correction");

        Assert.AreEqual(3, item.Log.Count);
    }
}
```

### 3. Run domain tests

```bash
cd src/backend
dotnet test Inventory/ECommerce.Inventory.Tests/ECommerce.Inventory.Tests.csproj \
    --filter "FullyQualifiedName~StockLevelTests|FullyQualifiedName~InventoryItemTests"
```

---

## Acceptance Criteria

- [ ] `ECommerce.Inventory.Tests` project created and added to solution
- [ ] `StockLevelTests.cs` — all tests pass
  - Covers: Create (valid/zero/negative), Reduce (valid/exact/insufficient/zero/negative amount), Increase (valid/zero/negative), equality
- [ ] `InventoryItemTests.cs` — all tests pass
  - Covers: Create invariants, Reduce/Increase/Adjust behavior, Log accumulation, domain event assertions
- [ ] `LowStockDetectedEvent` threshold crossing logic fully tested:
  - Crosses from above → fires
  - Already below threshold → does NOT fire
  - Exactly at threshold (previous) → does NOT fire
  - Reduces to exactly threshold → fires
- [ ] `Adjust` confirmed to raise ZERO domain events
- [ ] All tests are fast (no I/O, no database)
