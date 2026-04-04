# Phase 7, Step 1: Domain Project

**Prerequisite**: Steps 0 and 0b characterization tests pass.

Create `ECommerce.Orders.Domain` — aggregate roots, value objects, domain services, and repository interfaces.

---

## Task 1: Create the project

```bash
cd src/backend
dotnet new classlib -n ECommerce.Orders.Domain -o ECommerce.Orders.Domain
dotnet sln ECommerce.sln add ECommerce.Orders.Domain/ECommerce.Orders.Domain.csproj
dotnet add ECommerce.Orders.Domain/ECommerce.Orders.Domain.csproj reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
rm ECommerce.Orders.Domain/Class1.cs
```

---

## Task 2: Define Enums and Errors

**File: `ECommerce.Orders.Domain/Enums/OrderStatus.cs`**

```csharp
namespace ECommerce.Orders.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,      // Order created, awaiting confirmation
    Confirmed = 1,    // Payment confirmed, awaiting fulfillment
    Shipped = 2,      // Order shipped, tracking number provided
    Delivered = 3,    // Order delivered
    Cancelled = 4     // Order cancelled
}
```

**File: `ECommerce.Orders.Domain/OrdersErrors.cs`**

```csharp
using ECommerce.SharedKernel;

namespace ECommerce.Orders.Domain;

public static class OrdersErrors
{
    public static readonly DomainError OrderNotFound = 
        new DomainError("ORDER_NOT_FOUND", "Order does not exist");

    public static readonly DomainError OrderEmpty = 
        new DomainError("ORDER_EMPTY", "Order must contain at least one item");

    public static readonly DomainError OrderInvalidQuantity = 
        new DomainError("ORDER_INVALID_QUANTITY", "Order item quantity must be greater than zero");

    public static readonly DomainError OrderInvalidPrice = 
        new DomainError("ORDER_INVALID_PRICE", "Order item price must be greater than zero");

    public static readonly DomainError OrderCannotCancelShipped = 
        new DomainError("ORDER_CANNOT_CANCEL_SHIPPED", "Cannot cancel an order that has already shipped");

    public static readonly DomainError OrderCannotCancelDelivered = 
        new DomainError("ORDER_CANNOT_CANCEL_DELIVERED", "Cannot cancel an order that has been delivered");

    public static readonly DomainError OrderCannotShipPending = 
        new DomainError("ORDER_CANNOT_SHIP_PENDING", "Cannot ship a pending order; must confirm first");

    public static readonly DomainError OrderAlreadyConfirmed = 
        new DomainError("ORDER_ALREADY_CONFIRMED", "Order is already confirmed");

    public static readonly DomainError OrderNumberInvalid = 
        new DomainError("ORDER_NUMBER_INVALID", "Invalid order number format");

    public static readonly DomainError ConcurrencyConflict = 
        new DomainError("CONCURRENCY_CONFLICT", "Order was modified by another user");
}
```

---

## Task 3: Define Value Objects

**File: `ECommerce.Orders.Domain/ValueObjects/OrderNumber.cs`**

```csharp
using ECommerce.SharedKernel;

namespace ECommerce.Orders.Domain.ValueObjects;

public record OrderNumber
{
    public string Value { get; }

    private OrderNumber(string value) => Value = value;

    public static Result<OrderNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<OrderNumber>.Failure(OrdersErrors.OrderNumberInvalid);

        // Format: ORD-YYYYMMDD-NNNNNN
        if (!value.StartsWith("ORD-") || value.Length != 18)
            return Result<OrderNumber>.Failure(OrdersErrors.OrderNumberInvalid);

        return Result<OrderNumber>.Ok(new OrderNumber(value));
    }

    public static OrderNumber Reconstitute(string value) => new(value);

    public override string ToString() => Value;
}
```

**File: `ECommerce.Orders.Domain/ValueObjects/Money.cs`**

```csharp
using ECommerce.SharedKernel;

namespace ECommerce.Orders.Domain.ValueObjects;

public record Money
{
    public decimal Amount { get; }

    private Money(decimal amount) => Amount = amount;

    public static Result<Money> Create(decimal amount)
    {
        if (amount < 0)
            return Result<Money>.Failure(OrdersErrors.OrderInvalidPrice);

        return Result<Money>.Ok(new Money(Math.Round(amount, 2)));
    }

    public static Money Reconstitute(decimal amount) => new(amount);

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public override string ToString() => $"${Amount:F2}";
}
```

**File: `ECommerce.Orders.Domain/ValueObjects/Quantity.cs`**

```csharp
using ECommerce.SharedKernel;

namespace ECommerce.Orders.Domain.ValueObjects;

public record Quantity
{
    public int Value { get; }

    private Quantity(int value) => Value = value;

    public static Result<Quantity> Create(int value)
    {
        if (value <= 0)
            return Result<Quantity>.Failure(OrdersErrors.OrderInvalidQuantity);

        return Result<Quantity>.Ok(new Quantity(value));
    }

    public static Quantity Reconstitute(int value) => new(value);

    public override string ToString() => Value.ToString();
}
```

---

## Task 4: Define Owned Entity (Order Line Item)

**File: `ECommerce.Orders.Domain/Aggregates/Order/OrderLineItem.cs`**

```csharp
using ECommerce.Orders.Domain.ValueObjects;
using ECommerce.SharedKernel;

namespace ECommerce.Orders.Domain.Aggregates.Order;

public class OrderLineItem
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Quantity Quantity { get; private set; } = null!;
    public Money UnitPrice { get; private set; } = null!;

    private OrderLineItem() { } // For EF

    internal OrderLineItem(Guid productId, Quantity quantity, Money unitPrice)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Money LineTotal => UnitPrice with { Amount = UnitPrice.Amount * Quantity.Value };
}
```

---

## Task 5: Define the Aggregate

**File: `ECommerce.Orders.Domain/Aggregates/Order/Order.cs`**

```csharp
using ECommerce.Orders.Domain.Enums;
using ECommerce.Orders.Domain.ValueObjects;
using ECommerce.SharedKernel;

namespace ECommerce.Orders.Domain.Aggregates.Order;

public class Order : AggregateRoot
{
    public Guid CustomerId { get; private set; }
    public OrderNumber OrderNumber { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public List<OrderLineItem> Items { get; private set; } = new();
    public Money Subtotal { get; private set; } = null!;
    public Money Tax { get; private set; } = null!;
    public Money ShippingCost { get; private set; } = null!;
    public Money Total { get; private set; } = null!;
    public string? TrackingNumber { get; private set; }
    public string? CancellationReason { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    private Order() { } // For EF

    internal Order(
        Guid id,
        Guid customerId,
        OrderNumber orderNumber,
        List<OrderLineItem> items,
        Money subtotal,
        Money tax,
        Money shippingCost)
    {
        Id = id;
        CustomerId = customerId;
        OrderNumber = orderNumber;
        Items = items;
        Subtotal = subtotal;
        Tax = tax;
        ShippingCost = shippingCost;
        Total = subtotal + tax + shippingCost;
        Status = OrderStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory: Create a new order
    /// </summary>
    public static Result<Order> Create(
        Guid customerId,
        OrderNumber orderNumber,
        List<OrderLineItem> items,
        Money subtotal,
        Money tax,
        Money shippingCost)
    {
        if (!items.Any())
            return Result<Order>.Failure(OrdersErrors.OrderEmpty);

        var order = new Order(Guid.NewGuid(), customerId, orderNumber, items, subtotal, tax, shippingCost);
        order.RaiseDomainEvent(new OrderPlacedEvent(order.Id, customerId, order.Total.Amount));
        return Result<Order>.Ok(order);
    }

    /// <summary>
    /// Confirm the order (payment verified)
    /// </summary>
    public Result Confirm()
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure(OrdersErrors.OrderAlreadyConfirmed);

        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new OrderConfirmedEvent(Id));
        return Result.Ok();
    }

    /// <summary>
    /// Ship the order with tracking number
    /// </summary>
    public Result Ship(string trackingNumber)
    {
        if (Status != OrderStatus.Confirmed)
            return Result.Failure(OrdersErrors.OrderCannotShipPending);

        Status = OrderStatus.Shipped;
        TrackingNumber = trackingNumber;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new OrderShippedEvent(Id, trackingNumber));
        return Result.Ok();
    }

    /// <summary>
    /// Mark order as delivered
    /// </summary>
    public void Deliver()
    {
        Status = OrderStatus.Delivered;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new OrderDeliveredEvent(Id));
    }

    /// <summary>
    /// Cancel the order (only allowed before shipping)
    /// </summary>
    public Result Cancel(string? reason)
    {
        if (Status == OrderStatus.Shipped)
            return Result.Failure(OrdersErrors.OrderCannotCancelShipped);

        if (Status == OrderStatus.Delivered)
            return Result.Failure(OrdersErrors.OrderCannotCancelDelivered);

        if (Status == OrderStatus.Cancelled)
            return Result.Ok(); // Idempotent

        Status = OrderStatus.Cancelled;
        CancellationReason = reason;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new OrderCancelledEvent(Id, reason));
        return Result.Ok();
    }
}
```

---

## Task 6: Define Domain Events

**File: `ECommerce.Orders.Domain/Aggregates/Order/OrderPlacedEvent.cs`**

```csharp
using ECommerce.SharedKernel;

namespace ECommerce.Orders.Domain.Aggregates.Order;

public record OrderPlacedEvent(Guid OrderId, Guid CustomerId, decimal TotalAmount) : DomainEvent;
```

**File: `ECommerce.Orders.Domain/Aggregates/Order/OrderConfirmedEvent.cs`**

```csharp
using ECommerce.SharedKernel;

namespace ECommerce.Orders.Domain.Aggregates.Order;

public record OrderConfirmedEvent(Guid OrderId) : DomainEvent;
```

**File: `ECommerce.Orders.Domain/Aggregates/Order/OrderShippedEvent.cs`**

```csharp
using ECommerce.SharedKernel;

namespace ECommerce.Orders.Domain.Aggregates.Order;

public record OrderShippedEvent(Guid OrderId, string TrackingNumber) : DomainEvent;
```

**File: `ECommerce.Orders.Domain/Aggregates/Order/OrderDeliveredEvent.cs`**

```csharp
using ECommerce.SharedKernel;

namespace ECommerce.Orders.Domain.Aggregates.Order;

public record OrderDeliveredEvent(Guid OrderId) : DomainEvent;
```

**File: `ECommerce.Orders.Domain/Aggregates/Order/OrderCancelledEvent.cs`**

```csharp
using ECommerce.SharedKernel;

namespace ECommerce.Orders.Domain.Aggregates.Order;

public record OrderCancelledEvent(Guid OrderId, string? Reason) : DomainEvent;
```

---

## Task 7: Define Repository Interface

**File: `ECommerce.Orders.Domain/Interfaces/IOrderRepository.cs`**

```csharp
using ECommerce.Orders.Domain.Aggregates.Order;

namespace ECommerce.Orders.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default);

    Task<(List<Order> Items, int TotalCount)> GetByCustomerAsync(
        Guid customerId, int page, int pageSize, CancellationToken ct = default);

    Task<(List<Order> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? status, CancellationToken ct = default);

    Task<(List<Order> Items, int TotalCount)> GetPendingAsync(
        int page, int pageSize, CancellationToken ct = default);

    Task UpsertAsync(Order order, CancellationToken ct = default);

    Task DeleteAsync(Order order, CancellationToken ct = default);
}
```

---

## Task 8: Assembly Visibility

Add to `ECommerce.Orders.Domain/Properties/AssemblyInfo.cs`:

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ECommerce.Orders.Application")]
[assembly: InternalsVisibleTo("ECommerce.Orders.Infrastructure")]
[assembly: InternalsVisibleTo("ECommerce.Tests")]
```

---

## Acceptance Criteria

- [ ] Project builds with zero errors
- [ ] `OrderStatus` enum has: Pending, Confirmed, Shipped, Delivered, Cancelled
- [ ] `OrdersErrors` static class defines all error codes
- [ ] `OrderNumber` value object validates format (ORD-YYYYMMDD-NNNNNN)
- [ ] `Money` value object validates non-negative amounts, supports addition
- [ ] `Quantity` value object validates > 0
- [ ] `OrderLineItem` owned entity has ProductId, Quantity, UnitPrice, LineTotal calculation
- [ ] `Order` aggregate has: CustomerId, OrderNumber, Items, Subtotal, Tax, ShippingCost, Total, TrackingNumber, Status, RowVersion
- [ ] `Create()` factory returns `Result<Order>` and raises `OrderPlacedEvent`
- [ ] `Confirm()` transitions Pending → Confirmed, raises event
- [ ] `Ship(trackingNumber)` transitions Confirmed → Shipped, requires Confirmed status
- [ ] `Cancel()` only allowed before shipping, raises event
- [ ] `Deliver()` transitions to Delivered, raises event
- [ ] `IOrderRepository` interface defined with 7 methods
- [ ] `AssemblyInfo` includes visibility grants for Application, Infrastructure, Tests
