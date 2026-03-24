# Phase 3: Inventory Bounded Context

**Prerequisite**: Phase 2 complete.

**Learn**: Domain Events end-to-end, cross-context communication by ID, extracting a new aggregate from an existing entity, event handlers as side-effect managers.

---

## What's New in This Phase

Phases 1–2 defined and raised domain events, but the events had no handlers — they just fired into the void. This phase introduces the **full domain event lifecycle**:

1. **Raise** an event in the aggregate
2. **Dispatch** it after `SaveChangesAsync` (Phase 0 infrastructure handles this)
3. **Handle** it in a separate class that does one thing
4. **React** to events from OTHER contexts (Ordering's `OrderPlacedEvent` triggers stock reduction)

Also: `InventoryItem` is a **new aggregate** extracted from `Product.StockQuantity`. This is the DDD concept of **context separation in action** — stock levels belong to Inventory, not Catalog.

---

## The Critical Design Decision: Extracting InventoryItem

Currently `Product` has `StockQuantity` and `LowStockThreshold` properties. In DDD:

- **Catalog context**: Product is about description, pricing, presentation. A marketing team manages it.
- **Inventory context**: InventoryItem is about stock levels, replenishment, thresholds. A warehouse team manages it.

These change for different reasons and are owned by different people. They must be separate aggregates.

```
BEFORE (anemic, single context):
Product { Id, Name, Price, StockQuantity, LowStockThreshold, ... }

AFTER (DDD, separate contexts):
Catalog:   Product { Id, Name, Price, ... }         ← no stock fields
Inventory: InventoryItem { Id, ProductId, Quantity, LowStockThreshold, ... }
```

`InventoryItem` references `ProductId` (a Guid), NOT a navigation property to `Product`. Cross-context by ID only (Rule 2).

**Migration concern**: When you create `InventoryItem`, you must migrate `Product.StockQuantity` data into it. Write a one-time migration script. Do NOT silently lose stock data.

---

## Old Service → New Handler Mapping

| Old Method | New Handler |
|-----------|-------------|
| `InventoryService.GetInventoryAsync()` | `GetInventoryQuery` |
| `InventoryService.GetInventoryByProductIdAsync(id)` | `GetInventoryByProductIdQuery` |
| `InventoryService.GetLowStockItemsAsync()` | `GetLowStockItemsQuery` |
| `InventoryService.AddStockAsync(productId, qty)` | `IncreaseStockCommand` |
| `InventoryService.ReduceStockAsync(productId, qty)` | `ReduceStockCommand` |
| `InventoryService.AdjustStockAsync(productId, qty, reason)` | `AdjustStockCommand` |
| *(via OrderService internally)* | `ReduceStockOnOrderPlacedHandler` (event handler) |

---

## Step 1: Domain Project

### Value Objects

```csharp
// ValueObjects/StockLevel.cs
public record StockLevel
{
    public int Quantity { get; }

    private StockLevel(int quantity) => Quantity = quantity;

    public static StockLevel Create(int quantity)
    {
        if (quantity < 0)
            throw new InventoryDomainException("STOCK_NEGATIVE", "Stock quantity cannot be negative.");
        return new StockLevel(quantity);
    }

    public static StockLevel Zero => new(0);

    public StockLevel Reduce(int amount)
    {
        if (amount <= 0)
            throw new InventoryDomainException("REDUCE_AMOUNT_INVALID", "Amount to reduce must be positive.");
        if (Quantity - amount < 0)
            throw new InventoryDomainException("INSUFFICIENT_STOCK", "Insufficient stock to reduce.");
        return new StockLevel(Quantity - amount);
    }

    public StockLevel Increase(int amount)
    {
        if (amount <= 0)
            throw new InventoryDomainException("INCREASE_AMOUNT_INVALID", "Amount to increase must be positive.");
        return new StockLevel(Quantity + amount);
    }
}
```

Notice: `StockLevel` has **behavior** — `Reduce` and `Increase` return new instances. Value objects can have behavior (Rule 11).

### Domain events

```csharp
public record StockReducedEvent(
    Guid InventoryItemId,
    Guid ProductId,
    int QuantityReduced,
    int NewQuantity,
    string Reason
) : DomainEventBase;

public record LowStockDetectedEvent(
    Guid ProductId,
    int CurrentStock,
    int Threshold
) : DomainEventBase;

public record StockReplenishedEvent(
    Guid ProductId,
    int QuantityAdded,
    int NewQuantity
) : DomainEventBase;
```

### InventoryItem aggregate

```csharp
// Aggregates/InventoryItem/InventoryItem.cs
public class InventoryItem : AggregateRoot
{
    public Guid ProductId { get; private set; }  // ID only — no navigation to Product
    public StockLevel Stock { get; private set; } = null!;
    public int LowStockThreshold { get; private set; }
    public bool TrackInventory { get; private set; }

    private readonly List<InventoryLog> _log = new();
    public IReadOnlyCollection<InventoryLog> Log => _log.AsReadOnly();

    private InventoryItem() { }

    public static InventoryItem Create(Guid productId, int initialQuantity, int lowStockThreshold)
    {
        if (lowStockThreshold < 0)
            throw new InventoryDomainException("THRESHOLD_NEGATIVE", "Low stock threshold cannot be negative.");

        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Stock = StockLevel.Create(initialQuantity),
            LowStockThreshold = lowStockThreshold,
            TrackInventory = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return item;
    }

    public void Reduce(int amount, string reason)
    {
        var previous = Stock;
        Stock = Stock.Reduce(amount);  // throws if insufficient

        _log.Add(InventoryLog.Create(Id, -amount, reason, Stock.Quantity));
        AddDomainEvent(new StockReducedEvent(Id, ProductId, amount, Stock.Quantity, reason));

        // Check low stock AFTER reducing
        if (Stock.Quantity <= LowStockThreshold && previous.Quantity > LowStockThreshold)
            AddDomainEvent(new LowStockDetectedEvent(ProductId, Stock.Quantity, LowStockThreshold));
    }

    public void Increase(int amount, string reason)
    {
        Stock = Stock.Increase(amount);
        _log.Add(InventoryLog.Create(Id, amount, reason, Stock.Quantity));
        AddDomainEvent(new StockReplenishedEvent(ProductId, amount, Stock.Quantity));
    }

    public void Adjust(int newQuantity, string reason)
    {
        var delta = newQuantity - Stock.Quantity;
        Stock = StockLevel.Create(newQuantity);
        _log.Add(InventoryLog.Create(Id, delta, reason, Stock.Quantity));
    }
}
```

**Key insight**: `LowStockDetectedEvent` is only raised when stock CROSSES the threshold (from above to below), not on every reduction. `previous.Quantity > LowStockThreshold && Stock.Quantity <= LowStockThreshold` — this is a classic "state transition" check.

---

## Step 2: Application Project — Event Handlers

This is the new piece. Event handlers live in `EventHandlers/` in the Application project.

### Handling a domain event from THIS context

```csharp
// EventHandlers/SendLowStockAlertOnLowStockDetectedHandler.cs
public class SendLowStockAlertOnLowStockDetectedHandler
    : INotificationHandler<LowStockDetectedEvent>
{
    private readonly IEmailService _email;
    private readonly ILogger<SendLowStockAlertOnLowStockDetectedHandler> _logger;

    public async Task Handle(LowStockDetectedEvent notification, CancellationToken ct)
    {
        try
        {
            await _email.SendLowStockAlertAsync(
                notification.ProductId,
                notification.CurrentStock,
                notification.Threshold,
                ct);
        }
        catch (Exception ex)
        {
            // Rule 17: handlers don't throw to callers — log and move on
            _logger.LogError(ex,
                "Failed to send low stock alert for ProductId {ProductId}",
                notification.ProductId);
        }
    }
}
```

### Handling a domain event from ANOTHER context

When an Order is placed (Phase 7), inventory must be reduced. But in Phase 3, the Ordering context doesn't exist yet. Here's how to handle this incrementally:

**Phase 3 approach**: Define the handler interface and a stub. Wire it up when Phase 7 arrives.

```csharp
// EventHandlers/ReduceStockOnOrderPlacedHandler.cs
// NOTE: OrderPlacedEvent will come from Ordering context (Phase 7).
// For now this handler is registered but OrderPlacedEvent doesn't exist yet.
// In Phase 7, add: cfg.RegisterServicesFromAssembly(typeof(ReduceStockCommand).Assembly)
// and the handler will automatically receive OrderPlacedEvent notifications.

// This is the SHAPE of the handler — implement fully in Phase 7.
// public class ReduceStockOnOrderPlacedHandler : INotificationHandler<OrderPlacedEvent>
// {
//     // Reduce stock for each OrderItem when order is placed
// }
```

**Why keep this stub in Phase 3?** Documentation. You're defining the contract now so the Programmer in Phase 7 knows exactly where to put the implementation.

### IEmailService interface

Notifications (email sending) are an **infrastructure concern**, not a domain service and not a bounded context. Define the interface in the Application project:

```csharp
// Interfaces/IEmailService.cs (in Inventory.Application)
public interface IEmailService
{
    Task SendLowStockAlertAsync(Guid productId, int currentStock, int threshold, CancellationToken ct);
}
```

Implementation lives in `Inventory.Infrastructure/Services/EmailService.cs` (calls SendGrid/SMTP).

**This is the resolution to the Notifications gap**: There is no Notifications bounded context. Email is a side effect handled by each context's event handlers calling `IEmailService`. If you need a full Notifications system (notification preferences, templates, history), add it as Phase 9 — but that's a separate decision.

---

## Step 3: Data Migration

When deploying Phase 3, you must move `Product.StockQuantity` data into `InventoryItem`. Write a one-time EF Core migration:

```csharp
// In the EF migration class:
protected override void Up(MigrationBuilder migrationBuilder)
{
    // 1. Create InventoryItems table (happens automatically from EF model)

    // 2. Seed InventoryItem for every existing Product
    migrationBuilder.Sql(@"
        INSERT INTO ""InventoryItems"" (""Id"", ""ProductId"", ""Quantity"", ""LowStockThreshold"", ...)
        SELECT gen_random_uuid(), ""Id"", ""StockQuantity"", ""LowStockThreshold"", ...
        FROM ""Products""
    ");

    // 3. Remove StockQuantity and LowStockThreshold from Products
    migrationBuilder.DropColumn("StockQuantity", "Products");
    migrationBuilder.DropColumn("LowStockThreshold", "Products");
}
```

This migration is irreversible in production. Test it on a staging database first.

Also update the `GetProductsQuery` in Catalog that hardcoded `InStock = true` in Phase 1:

```csharp
// Queries/GetProducts/GetProductsQueryHandler.cs — Phase 3 update
// Join to InventoryItems to get real stock status
.Select(p => new ProductDto(
    p.Id,
    p.Name.Value,
    p.Slug.Value,
    p.Price.Amount,
    p.Price.Currency,
    p.Category.Name.Value,
    inventoryItems.Any(i => i.ProductId == p.Id && i.Stock > 0),  // ← real stock
    p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault()))
```

---

## Definition of Done

- [ ] Characterization tests written against old InventoryService
- [ ] `InventoryItem` aggregate with `Reduce`, `Increase`, `Adjust` domain methods
- [ ] `StockLevel` value object with `Reduce` and `Increase` behavior
- [ ] Domain events defined: `StockReducedEvent`, `LowStockDetectedEvent`, `StockReplenishedEvent`
- [ ] `LowStockDetectedEvent` raised only on threshold crossing (not every reduction)
- [ ] `SendLowStockAlertOnLowStockDetectedHandler` implemented — catches exceptions, doesn't rethrow
- [ ] `IEmailService` interface in Application, implementation in Infrastructure
- [ ] Data migration script moves `Product.StockQuantity` into `InventoryItem` table
- [ ] Catalog `GetProductsQuery` updated to join real `InventoryItem` stock data
- [ ] Old `InventoryService` deleted after characterization tests pass

## What You Learned in Phase 3

- Domain events have a full lifecycle: raise in aggregate → dispatch after save → handle as side effect
- Event handlers do ONE thing, catch exceptions, never rethrow to callers (Rule 17)
- Cross-context communication is by ID only — `InventoryItem.ProductId` not `InventoryItem.Product`
- Notifications are infrastructure side effects, not a bounded context
- Separating concerns: extracting stock data from Product into InventoryItem reflects real organizational boundaries
