# Phase 8, Step 1: Integration Event Contracts

**Prerequisite**: Steps 0 and 0b complete. All synchronous behavior documented and tested.

Create `ECommerce.Contracts` project for integration event contracts. These are the **public API** for cross-context communication and must be versioned carefully.

---

## Task 1: Create Contracts Project

```bash
cd src/backend
dotnet new classlib -n ECommerce.Contracts -o ECommerce.Contracts
dotnet sln ECommerce.sln add ECommerce.Contracts/ECommerce.Contracts.csproj
dotnet add ECommerce.Contracts/ECommerce.Contracts.csproj package MassTransit
rm ECommerce.Contracts/Class1.cs
```

---

## Task 2: Define Integration Events

**File: `ECommerce.Contracts/OrderPlacedIntegrationEvent.cs`**

```csharp
namespace ECommerce.Contracts;

/// <summary>
/// Published by: Ordering context
/// Subscribed by: Inventory, Notifications, Cart (for clearing)
/// 
/// Schema version: 1.0
/// Breaking change: Adding required fields requires V2
/// </summary>
public record OrderPlacedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    Guid[] ProductIds,
    decimal TotalAmount,
    DateTime OccurredAt = default) 
    : IntegrationEvent
{
    public OrderPlacedIntegrationEvent() : this(Guid.Empty, Guid.Empty, Array.Empty<Guid>(), 0) { }
}
```

**File: `ECommerce.Contracts/InventoryReservedIntegrationEvent.cs`**

```csharp
namespace ECommerce.Contracts;

/// <summary>
/// Published by: Inventory context (in response to OrderPlaced)
/// Subscribed by: Ordering context, Notifications
/// </summary>
public record InventoryReservedIntegrationEvent(
    Guid OrderId,
    Guid[] ProductIds,
    int[] Quantities,
    DateTime OccurredAt = default)
    : IntegrationEvent
{
    public InventoryReservedIntegrationEvent() 
        : this(Guid.Empty, Array.Empty<Guid>(), Array.Empty<int>()) { }
}
```

**File: `ECommerce.Contracts/InventoryReservationFailedIntegrationEvent.cs`**

```csharp
namespace ECommerce.Contracts;

/// <summary>
/// Published by: Inventory context (when reservation fails)
/// Subscribed by: Ordering context (triggers saga compensation)
/// </summary>
public record InventoryReservationFailedIntegrationEvent(
    Guid OrderId,
    Guid ProductId,
    string Reason,
    DateTime OccurredAt = default)
    : IntegrationEvent
{
    public InventoryReservationFailedIntegrationEvent() 
        : this(Guid.Empty, Guid.Empty, "") { }
}
```

**File: `ECommerce.Contracts/PromoCodeAppliedIntegrationEvent.cs`**

```csharp
namespace ECommerce.Contracts;

/// <summary>
/// Published by: Promotions context (when order uses a promo)
/// Subscribed by: Ordering, Analytics
/// </summary>
public record PromoCodeAppliedIntegrationEvent(
    Guid PromoCodeId,
    string Code,
    decimal DiscountAmount,
    DateTime OccurredAt = default)
    : IntegrationEvent
{
    public PromoCodeAppliedIntegrationEvent() 
        : this(Guid.Empty, "", 0) { }
}
```

**File: `ECommerce.Contracts/IntegrationEvent.cs`**

```csharp
namespace ECommerce.Contracts;

/// <summary>
/// Base class for all integration events (messages published to broker)
/// </summary>
public abstract record IntegrationEvent
{
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}
```

---

## Task 3: Versioning Strategy

Events are **versioned by their contract**, not by assembly version.

**Rule 1**: If you add an **optional** field (with default), that's backward compatible — don't version.

**Rule 2**: If you add a **required** field, create a new event type and keep the old one:
```csharp
// OLD (v1) - still supported
public record OrderPlacedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount);

// NEW (v2) - with additional required field
public record OrderPlacedIntegrationEventV2(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    Guid ShippingAddressId); // New required field
```

**Rule 3**: When a consumer subscribes to a new event type, it must handle both versions:
```csharp
var endpoint = Endpoint.MapCompile<OrderPlacedIntegrationEvent>(async (context, message) => {
    // Handle v1
});

var endpointV2 = Endpoint.MapCompile<OrderPlacedIntegrationEventV2>(async (context, message) => {
    // Handle v2
});
```

---

## Task 4: Idempotency Keys

Integration events can be delivered multiple times. Consumers must be idempotent.

Add to all events:
```csharp
public Guid IdempotencyKey { get; set; } = Guid.NewGuid();
```

Consumer tracks processed keys:
```csharp
// Outbox table includes IdempotencyKey column
// Before processing, check: SELECT * FROM OutboxMessages WHERE IdempotencyKey = @key
// If found and Processed = true, skip
```

---

## Acceptance Criteria

- [ ] `ECommerce.Contracts` project created
- [ ] All critical integration events defined (OrderPlaced, InventoryReserved, InventoryReservationFailed, PromoCodeApplied, at minimum)
- [ ] Each event has: CorrelationId, PublishedAt, OccurredAt
- [ ] Each event has IdempotencyKey for deduplication
- [ ] Versioning strategy documented
- [ ] Event contracts are immutable records
- [ ] All downstream projects reference `ECommerce.Contracts`
