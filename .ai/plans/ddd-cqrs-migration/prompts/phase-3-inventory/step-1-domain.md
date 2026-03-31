# Phase 3, Step 1: Inventory Domain Project

**Prerequisite**: Phase 2 (Identity) is complete and all tests pass.

---

## Context

We are migrating the Inventory bounded context from `InventoryService` to DDD/CQRS. This step creates the Domain project only — no Application or Infrastructure yet.

**New concepts introduced in this phase:**
- `StockLevel` is a value object with **behavior** — `Reduce` and `Increase` return new instances (immutable).
- `InventoryItem` is a NEW aggregate extracted from `Product.StockQuantity`. Stock levels belong to Inventory, not Catalog. The aggregate holds a `ProductId` (Guid), NOT a navigation property to `Product`.
- `InventoryLog` is a child entity — each stock operation appends an immutable log entry.
- Domain events have a full lifecycle here: raised in the aggregate, dispatched after save, handled as side effects.
- `LowStockDetectedEvent` is raised only on threshold CROSSING (from above to below), not on every reduction.

---

## The Critical Design Decision: Extracting InventoryItem

```
BEFORE (anemic, single context):
Product { Id, Name, Price, StockQuantity, LowStockThreshold, ... }

AFTER (DDD, separate contexts):
Catalog:   Product { Id, Name, Price, ... }         ← no stock fields
Inventory: InventoryItem { Id, ProductId, Quantity, LowStockThreshold, ... }
```

`InventoryItem.ProductId` is a `Guid` — cross-context by ID only (Rule 2).

---

## Task: Create ECommerce.Inventory.Domain Project

### 1. Create the project

```bash
cd src/backend
mkdir -p Inventory
dotnet new classlib -n ECommerce.Inventory.Domain -f net10.0 -o Inventory/ECommerce.Inventory.Domain
dotnet sln ../../ECommerce.sln add Inventory/ECommerce.Inventory.Domain/ECommerce.Inventory.Domain.csproj

# Only dependency: SharedKernel
dotnet add Inventory/ECommerce.Inventory.Domain/ECommerce.Inventory.Domain.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj

# Delete auto-generated file
rm Inventory/ECommerce.Inventory.Domain/Class1.cs
```

### 2. Create domain errors

**File: `Inventory/ECommerce.Inventory.Domain/Errors/InventoryErrors.cs`**

```csharp
using ECommerce.SharedKernel.Results;

namespace ECommerce.Inventory.Domain.Errors;

public static class InventoryErrors
{
    // StockLevel value object
    public static readonly DomainError StockNegative         = new("STOCK_NEGATIVE",          "Stock quantity cannot be negative.");
    public static readonly DomainError ReduceAmountInvalid   = new("REDUCE_AMOUNT_INVALID",   "Reduction amount must be greater than zero.");
    public static readonly DomainError IncreaseAmountInvalid = new("INCREASE_AMOUNT_INVALID", "Increase amount must be greater than zero.");
    public static readonly DomainError InsufficientStock     = new("INSUFFICIENT_STOCK",      "Insufficient stock to complete this operation.");

    // InventoryItem aggregate
    public static readonly DomainError ThresholdNegative = new("THRESHOLD_NEGATIVE", "Low stock threshold cannot be negative.");

    // NOTE: InventoryItemNotFound requires a repository lookup — it lives in
    // InventoryApplicationErrors (step-2), not here.
}
```

### 3. Create StockLevel value object

**File: `Inventory/ECommerce.Inventory.Domain/ValueObjects/StockLevel.cs`**

```csharp
using ECommerce.SharedKernel.Results;
using ECommerce.Inventory.Domain.Errors;

namespace ECommerce.Inventory.Domain.ValueObjects;

// Single-property VO with behavior → use sealed record.
// Immutable: Reduce/Increase return NEW instances rather than mutating.
public sealed record StockLevel
{
    public int Quantity { get; }

    private StockLevel(int quantity) => Quantity = quantity;

    public static Result<StockLevel> Create(int quantity)
    {
        if (quantity < 0)
            return Result<StockLevel>.Fail(InventoryErrors.StockNegative);
        return Result<StockLevel>.Ok(new StockLevel(quantity));
    }

    public static StockLevel Zero => new(0);

    public Result<StockLevel> Reduce(int amount)
    {
        if (amount <= 0)
            return Result<StockLevel>.Fail(InventoryErrors.ReduceAmountInvalid);
        if (Quantity - amount < 0)
            return Result<StockLevel>.Fail(InventoryErrors.InsufficientStock);
        return Result<StockLevel>.Ok(new StockLevel(Quantity - amount));
    }

    public Result<StockLevel> Increase(int amount)
    {
        if (amount <= 0)
            return Result<StockLevel>.Fail(InventoryErrors.IncreaseAmountInvalid);
        return Result<StockLevel>.Ok(new StockLevel(Quantity + amount));
    }
}
```

### 4. Create domain events

**File: `Inventory/ECommerce.Inventory.Domain/Events/StockReducedEvent.cs`**
```csharp
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Inventory.Domain.Events;

public record StockReducedEvent(
    Guid InventoryItemId,
    Guid ProductId,
    int  QuantityReduced,
    int  NewQuantity,
    string Reason
) : DomainEventBase;
```

**File: `Inventory/ECommerce.Inventory.Domain/Events/LowStockDetectedEvent.cs`**
```csharp
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Inventory.Domain.Events;

public record LowStockDetectedEvent(
    Guid ProductId,
    int  CurrentStock,
    int  Threshold
) : DomainEventBase;
```

**File: `Inventory/ECommerce.Inventory.Domain/Events/StockReplenishedEvent.cs`**
```csharp
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Inventory.Domain.Events;

public record StockReplenishedEvent(
    Guid ProductId,
    int  QuantityAdded,
    int  NewQuantity
) : DomainEventBase;
```

### 5. Create InventoryLog child entity

**File: `Inventory/ECommerce.Inventory.Domain/Aggregates/InventoryItem/InventoryLog.cs`**

```csharp
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Inventory.Domain.Aggregates.InventoryItem;

// Child entity — append-only log of every stock operation.
// public sealed with internal constructor — same pattern as Catalog ProductImage.
// The type is visible to all assemblies (Application needs it for InventoryItem.Log),
// but only InventoryItem can create entries (internal static Create factory).
public sealed class InventoryLog : Entity
{
    public Guid     InventoryItemId { get; private set; }
    public int      Delta           { get; private set; } // positive = added, negative = removed
    public string   Reason          { get; private set; } = null!;
    public int      StockAfter      { get; private set; }
    public DateTime OccurredAt      { get; private set; }

    private InventoryLog() { } // EF Core

    internal static InventoryLog Create(Guid inventoryItemId, int delta, string reason, int stockAfter)
        => new()
        {
            Id              = Guid.NewGuid(),
            InventoryItemId = inventoryItemId,
            Delta           = delta,
            Reason          = reason,
            StockAfter      = stockAfter,
            OccurredAt      = DateTime.UtcNow,
        };
}
```

### 6. Create InventoryItem aggregate

**File: `Inventory/ECommerce.Inventory.Domain/Aggregates/InventoryItem/InventoryItem.cs`**

```csharp
using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;
using ECommerce.Inventory.Domain.Errors;
using ECommerce.Inventory.Domain.Events;
using ECommerce.Inventory.Domain.ValueObjects;

namespace ECommerce.Inventory.Domain.Aggregates.InventoryItem;

public sealed class InventoryItem : AggregateRoot
{
    public Guid       ProductId         { get; private set; }  // ID only — no navigation to Product
    public StockLevel Stock             { get; private set; } = null!;
    public int        LowStockThreshold { get; private set; }
    public bool       TrackInventory    { get; private set; }

    private readonly List<InventoryLog> _log = new();
    public IReadOnlyCollection<InventoryLog> Log => _log.AsReadOnly();

    private InventoryItem() { } // EF Core

    public static Result<InventoryItem> Create(Guid productId, int initialQuantity, int lowStockThreshold)
    {
        if (lowStockThreshold < 0)
            return Result<InventoryItem>.Fail(InventoryErrors.ThresholdNegative);

        var stockResult = StockLevel.Create(initialQuantity);
        if (!stockResult.IsSuccess) return Result<InventoryItem>.Fail(stockResult.GetErrorOrThrow());

        InventoryItem item = new()
        {
            ProductId         = productId,
            Stock             = stockResult.GetDataOrThrow(),
            LowStockThreshold = lowStockThreshold,
            TrackInventory    = true,
        };

        return Result<InventoryItem>.Ok(item);
    }

    public Result Reduce(int amount, string reason)
    {
        var previous    = Stock;
        var reduceResult = Stock.Reduce(amount);
        if (!reduceResult.IsSuccess) return Result.Fail(reduceResult.GetErrorOrThrow());

        Stock = reduceResult.GetDataOrThrow();
        _log.Add(InventoryLog.Create(Id, -amount, reason, Stock.Quantity));
        AddDomainEvent(new StockReducedEvent(Id, ProductId, amount, Stock.Quantity, reason));

        // Raise LowStockDetectedEvent ONLY when crossing the threshold (not every reduction)
        if (Stock.Quantity <= LowStockThreshold && previous.Quantity > LowStockThreshold)
            AddDomainEvent(new LowStockDetectedEvent(ProductId, Stock.Quantity, LowStockThreshold));

        return Result.Ok();
    }

    public Result Increase(int amount, string reason)
    {
        var increaseResult = Stock.Increase(amount);
        if (!increaseResult.IsSuccess) return Result.Fail(increaseResult.GetErrorOrThrow());

        Stock = increaseResult.GetDataOrThrow();
        _log.Add(InventoryLog.Create(Id, amount, reason, Stock.Quantity));
        AddDomainEvent(new StockReplenishedEvent(ProductId, amount, Stock.Quantity));
        return Result.Ok();
    }

    public Result Adjust(int newQuantity, string reason)
    {
        var stockResult = StockLevel.Create(newQuantity);
        if (!stockResult.IsSuccess) return Result.Fail(stockResult.GetErrorOrThrow());

        int delta = newQuantity - Stock.Quantity;
        Stock = stockResult.GetDataOrThrow();
        _log.Add(InventoryLog.Create(Id, delta, reason, Stock.Quantity));
        // Adjust does not raise domain events — it's an admin override
        return Result.Ok();
    }
}
```

**Key design notes:**
- `LowStockDetectedEvent` is raised only when stock CROSSES the threshold from above to below: `previous.Quantity > LowStockThreshold && Stock.Quantity <= LowStockThreshold`. This prevents duplicate alerts on every reduction when stock is already low.
- `Adjust` does not raise events — it's an admin override (manual correction), not a business operation.
- `InventoryLog` is `public sealed class` with `internal` constructor and factory — the type must be public because `InventoryItem.Log` exposes it as `IReadOnlyCollection<InventoryLog>`, and Application reads those entries. Construction is locked to the `InventoryItem` aggregate via the `internal static Create` factory.

### 7. Create AssemblyInfo for InternalsVisibleTo

`InventoryLog` is `public sealed class`, so Application can access it without `InternalsVisibleTo`. Infrastructure still gets the attribute as a safeguard — it can also call `internal` factory/mutation methods if needed in the future.

**File: `Inventory/ECommerce.Inventory.Domain/Properties/AssemblyInfo.cs`**

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ECommerce.Inventory.Infrastructure")]
```

### 8. Create repository interface

**File: `Inventory/ECommerce.Inventory.Domain/Interfaces/IInventoryItemRepository.cs`**

```csharp
using ECommerce.Inventory.Domain.Aggregates.InventoryItem;

namespace ECommerce.Inventory.Domain.Interfaces;

public interface IInventoryItemRepository
{
    Task<InventoryItem?>  GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InventoryItem?>  GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<List<InventoryItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<InventoryItem>> GetLowStockAsync(int? thresholdOverride = null, CancellationToken cancellationToken = default);
    Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default);
}
```

### 9. Verify

```bash
cd src/backend
dotnet build Inventory/ECommerce.Inventory.Domain/ECommerce.Inventory.Domain.csproj
dotnet build  # Entire solution still builds
```

---

## Tester Handoff

Once this step is delivered, the tester writes domain unit tests in `ECommerce.Inventory.Tests/Domain/`. See `step-5-domain-tests.md`.

---

## Acceptance Criteria

- [ ] `ECommerce.Inventory.Domain` project created and added to solution
- [ ] Only dependency: `ECommerce.SharedKernel`
- [ ] No `using Microsoft.EntityFrameworkCore` anywhere in Domain
- [ ] `StockLevel` is a `sealed record` with `Reduce`, `Increase` behavior methods returning `Result<StockLevel>`
- [ ] `InventoryItem` aggregate is `sealed class` with `Create`, `Reduce`, `Increase`, `Adjust` methods
- [ ] `InventoryLog` is `public sealed class` with `internal` constructor and `internal static` factory — type is visible externally (Application reads `InventoryItem.Log`), but only `InventoryItem` can create entries
- [ ] `Properties/AssemblyInfo.cs` with `InternalsVisibleTo("ECommerce.Inventory.Infrastructure")`
- [ ] 3 domain events defined as records: `StockReducedEvent`, `LowStockDetectedEvent`, `StockReplenishedEvent`
- [ ] `LowStockDetectedEvent` raised only on threshold crossing — NOT on every reduction
- [ ] `InventoryErrors` has NO `InventoryItemNotFound` — that lives in `InventoryApplicationErrors` (step-2)
- [ ] `IInventoryItemRepository` has 5 methods
- [ ] `dotnet build` passes for Domain project and entire solution
