# Phase 7: Ordering Bounded Context

**Prerequisite**: Phases 1–6 complete.

**Learn**: State machine in a value object, OrderItem as a snapshot, PlaceOrder as the most complex command in the system, wiring all previous phase event handlers, Payment as an external system.

---

## Why Ordering Is Last

Order placement touches every other context:
- Catalog → snapshot product name + price into OrderItem
- Identity → snapshot delivery address from User
- Inventory → reduce stock on OrderPlaced
- Shopping → clear cart on OrderPlaced
- Promotions → apply promo code, record usage
- Reviews → mark as verified purchase on OrderDelivered

All of these event handler stubs (placed in phases 3, 4, 5, 6) are wired up here. This phase is the integration test of the entire migration.

---

## Key Design Decisions (Resolve Before Starting)

### Decision 1: Payment

**Decision**: Payment processing is external. The Order records the RESULT of payment, not the payment itself.

`PaymentInfo` is a value object on Order that records:
- `PaymentReference` — the ID from Stripe/PayPal
- `PaymentMethod` — Card, PayPal, etc.
- `PaidAmount` — the amount charged
- `PaidAt` — when payment occurred

The Order does NOT call Stripe. The API layer calls Stripe before dispatching `PlaceOrderCommand`. If Stripe fails, `PlaceOrderCommand` is never sent. If Stripe succeeds, the reference ID is passed into the command.

This keeps the domain clean of external payment SDK dependencies.

```csharp
// The PlaceOrder flow from the controller's perspective:
// 1. [Controller] Call Stripe → get PaymentIntentId
// 2. [Controller] Send PlaceOrderCommand(... paymentReference: paymentIntentId)
// 3. [Handler]    Create Order with PaymentInfo
// 4. [Handler]    Raise OrderPlacedEvent
```

### Decision 2: OrderStatus as Enumeration Class

`OrderStatus` has transition rules (Pending → Confirmed is valid; Pending → Shipped is not). Use the Enumeration class pattern (see `theory/04-value-types-and-dtos.md` §Enumeration Class).

### Decision 3: OrderItem is an immutable snapshot

When an order is placed, OrderItem copies the product name and price at that moment. Future Catalog changes don't affect historical orders. `OrderItem` has NO navigation property to `Product`.

---

## Old Service → New Handler Mapping

| Old Method | New Handler |
|-----------|-------------|
| `OrderService.GetOrdersAsync(filter)` | `GetOrdersQuery` (admin) |
| `OrderService.GetUserOrdersAsync(userId)` | `GetUserOrdersQuery` |
| `OrderService.GetOrderByIdAsync(id)` | `GetOrderByIdQuery` |
| `OrderService.PlaceOrderAsync(dto)` | `PlaceOrderCommand` ← most complex |
| `OrderService.ConfirmOrderAsync(id)` | `ConfirmOrderCommand` |
| `OrderService.ShipOrderAsync(id, tracking)` | `ShipOrderCommand` |
| `OrderService.DeliverOrderAsync(id)` | `DeliverOrderCommand` |
| `OrderService.CancelOrderAsync(id, reason)` | `CancelOrderCommand` |

---

## Step 1: Domain Project

### OrderStatus — Enumeration Class with Transition Logic

```csharp
// ValueObjects/OrderStatus.cs
public class OrderStatus : ValueObject
{
    public static readonly OrderStatus Pending    = new(1, "Pending");
    public static readonly OrderStatus Confirmed  = new(2, "Confirmed");
    public static readonly OrderStatus Processing = new(3, "Processing");
    public static readonly OrderStatus Shipped    = new(4, "Shipped");
    public static readonly OrderStatus Delivered  = new(5, "Delivered");
    public static readonly OrderStatus Cancelled  = new(6, "Cancelled");

    public int Id { get; }
    public string Name { get; }

    private static readonly IReadOnlyDictionary<int, OrderStatus> All = new Dictionary<int, OrderStatus>
    {
        { 1, Pending }, { 2, Confirmed }, { 3, Processing },
        { 4, Shipped }, { 5, Delivered }, { 6, Cancelled }
    };

    private OrderStatus() { }  // EF Core
    private OrderStatus(int id, string name) { Id = id; Name = name; }

    public static OrderStatus FromName(string name) =>
        All.Values.FirstOrDefault(s => s.Name == name)
            ?? throw new OrderingDomainException("STATUS_UNKNOWN", $"Unknown order status: {name}");

    public bool CanTransitionTo(OrderStatus next)
    {
        return (Id, next.Id) switch
        {
            (1, 2) => true,   // Pending → Confirmed
            (1, 6) => true,   // Pending → Cancelled
            (2, 3) => true,   // Confirmed → Processing
            (2, 6) => true,   // Confirmed → Cancelled
            (3, 4) => true,   // Processing → Shipped
            (3, 6) => true,   // Processing → Cancelled (partial refund)
            (4, 5) => true,   // Shipped → Delivered
            _ => false
        };
    }

    public override string ToString() => Name;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Id;
    }
}
```

**EF Core configuration**: `OrderStatus` is a `ValueObject` (class), so use `OwnsOne`. But since it's identified by `Name` (string), a value converter is cleaner:

```csharp
builder.Property(o => o.Status)
    .HasConversion(s => s.Name, v => OrderStatus.FromName(v))
    .HasMaxLength(50)
    .IsRequired();
```

### PaymentInfo value object

```csharp
public class PaymentInfo : ValueObject
{
    public string PaymentReference { get; }  // Stripe PaymentIntentId
    public string PaymentMethod { get; }     // "card", "paypal", etc.
    public decimal PaidAmount { get; }
    public DateTime PaidAt { get; }

    private PaymentInfo() { }
    private PaymentInfo(string reference, string method, decimal amount, DateTime paidAt)
    {
        PaymentReference = reference;
        PaymentMethod = method;
        PaidAmount = amount;
        PaidAt = paidAt;
    }

    public static PaymentInfo Create(string reference, string method, decimal amount, DateTime paidAt)
    {
        if (string.IsNullOrWhiteSpace(reference))
            throw new OrderingDomainException("PAYMENT_REF_EMPTY", "Payment reference cannot be empty.");
        if (amount <= 0)
            throw new OrderingDomainException("PAYMENT_AMOUNT_INVALID", "Payment amount must be positive.");
        return new PaymentInfo(reference, method, amount, paidAt);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PaymentReference;
    }
}
```

### Order aggregate

```csharp
// Aggregates/Order/Order.cs
public class Order : AggregateRoot
{
    public string OrderNumber { get; private set; } = null!;  // e.g., "ORD-2024-00001"
    public Guid UserId { get; private set; }                  // Reference to Identity

    // Address snapshots — not references to User.Addresses
    public ShippingAddress ShippingAddress { get; private set; } = null!;

    public OrderStatus Status { get; private set; } = null!;
    public PaymentInfo? Payment { get; private set; }
    public Guid? PromoCodeId { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal ShippingCost { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public static Order Place(
        Guid userId,
        ShippingAddress shippingAddress,
        IReadOnlyList<OrderItemData> items,
        decimal shippingCost,
        decimal taxAmount,
        PaymentInfo payment,
        decimal discountAmount = 0,
        Guid? promoCodeId = null)
    {
        if (!items.Any())
            throw new OrderingDomainException("ORDER_EMPTY", "Order must have at least one item.");

        var subtotal = items.Sum(i => i.UnitPrice * i.Quantity);
        var total = subtotal + shippingCost + taxAmount - discountAmount;

        if (total <= 0)
            throw new OrderingDomainException("ORDER_TOTAL_INVALID", "Order total must be positive.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            UserId = userId,
            ShippingAddress = shippingAddress,
            Status = OrderStatus.Pending,
            Payment = payment,
            PromoCodeId = promoCodeId,
            Subtotal = subtotal,
            DiscountAmount = discountAmount,
            ShippingCost = shippingCost,
            TaxAmount = taxAmount,
            Total = total,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var item in items)
            order._items.Add(OrderItem.Create(Guid.NewGuid(), order.Id, item));

        order.AddDomainEvent(new OrderPlacedEvent(order.Id, userId, order.Total, order.Items));
        return order;
    }

    public void Confirm()
    {
        TransitionTo(OrderStatus.Confirmed);
        AddDomainEvent(new OrderConfirmedEvent(Id, UserId));
    }

    public void Ship(string trackingNumber)
    {
        TransitionTo(OrderStatus.Shipped);
        AddDomainEvent(new OrderShippedEvent(Id, UserId, trackingNumber));
    }

    public void Deliver()
    {
        TransitionTo(OrderStatus.Delivered);
        AddDomainEvent(new OrderDeliveredEvent(Id, UserId,
            _items.Select(i => i.ProductId).ToList()));
    }

    public void Cancel(string reason)
    {
        TransitionTo(OrderStatus.Cancelled);
        AddDomainEvent(new OrderCancelledEvent(Id, UserId, reason));
    }

    private void TransitionTo(OrderStatus newStatus)
    {
        if (!Status.CanTransitionTo(newStatus))
            throw new OrderingDomainException(
                "ORDER_INVALID_TRANSITION",
                $"Cannot transition order from {Status} to {newStatus}.");
        Status = newStatus;
    }

    private static string GenerateOrderNumber() =>
        $"ORD-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
}

// Helper record passed to Order.Place() — not a domain object, just a data carrier
public record OrderItemData(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, string? ImageUrl);
```

### OrderItem — immutable snapshot

```csharp
// Aggregates/Order/OrderItem.cs
public class OrderItem : Entity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }       // ID reference to Catalog
    public string ProductName { get; private set; } = null!;  // Snapshot — won't change
    public decimal UnitPrice { get; private set; }    // Snapshot — won't change
    public int Quantity { get; private set; }
    public string? ProductImageUrl { get; private set; }  // Snapshot

    private OrderItem() { }

    internal static OrderItem Create(Guid id, Guid orderId, OrderItemData data)
    {
        return new OrderItem
        {
            Id = id,
            OrderId = orderId,
            ProductId = data.ProductId,
            ProductName = data.ProductName,  // ← snapshot
            UnitPrice = data.UnitPrice,      // ← snapshot
            Quantity = data.Quantity,
            ProductImageUrl = data.ImageUrl
        };
    }
}
```

**Tester handoff after Step 1:** Once the `Order` aggregate, `OrderStatus` enumeration class, and value objects are delivered, the tester writes domain unit tests in `ECommerce.Ordering.Tests/Domain/`. See `.ai/plans/ddd-cqrs-migration/testing/tester-prompt-template.md` → Prompt 2.

---

## Step 2: PlaceOrder — The Most Complex Command

```csharp
// Commands/PlaceOrder/PlaceOrderCommandHandler.cs
public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly AppDbContext _db;                // shared DB for cross-context reads
    private readonly IPromoCodeRepository _promoCodes; // Promotions context
    private readonly DiscountCalculator _discountCalc;

    public async Task<Result<OrderDto>> Handle(PlaceOrderCommand command, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? return Result<OrderDto>.Unauthorized();

        // 1. Load and validate cart items with product snapshots
        //    (cross-context: read from Catalog via shared DB)
        var productIds = command.CartItemIds.Select(x => x.ProductId).ToList();
        var products = await _db.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted && p.Status == "Active")
            .Select(p => new { p.Id, Name = p.Name.Value, Price = p.Price.Amount,
                               Currency = p.Price.Currency,
                               Image = p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault() })
            .ToListAsync(ct);

        if (products.Count != productIds.Distinct().Count())
            return Result<OrderDto>.Fail(ErrorCodes.Ordering.ProductsUnavailable, "One or more products are unavailable.");

        // Build snapshot items
        var orderItems = command.CartItemIds.Select(ci =>
        {
            var p = products.First(x => x.Id == ci.ProductId);
            return new OrderItemData(p.Id, p.Name, p.Price, ci.Quantity, p.Image);
        }).ToList();

        var subtotal = orderItems.Sum(i => i.UnitPrice * i.Quantity);

        // 2. Apply promo code if provided
        decimal discountAmount = 0;
        Guid? promoCodeId = null;
        PromoCode? promoCode = null;

        if (!string.IsNullOrEmpty(command.PromoCode))
        {
            promoCode = await _promoCodes.GetByCodeAsync(command.PromoCode, ct);
            if (promoCode is null)
                return Result<OrderDto>.Fail(ErrorCodes.Ordering.PromoCodeNotFound, "Promo code not found.");

            try
            {
                var calculation = _discountCalc.Calculate(promoCode, subtotal, DateTime.UtcNow);
                discountAmount = calculation.DiscountAmount;
                promoCodeId = promoCode.Id;
            }
            catch (PromotionsDomainException ex)
            {
                return Result<OrderDto>.Fail(ErrorCodes.Ordering.PromoCodeInvalid, ex.Message);
            }
        }

        // 3. Load shipping address snapshot from Identity context
        var address = await _db.Addresses.AsNoTracking()
            .Where(a => a.Id == command.ShippingAddressId && a.UserId == userId)
            .Select(a => new { a.Street, a.City, a.Country, a.PostalCode })
            .FirstOrDefaultAsync(ct);

        if (address is null)
            return Result<OrderDto>.Fail(ErrorCodes.Ordering.AddressNotFound, "Shipping address not found.");

        var shippingAddress = ShippingAddress.Create(
            address.Street, address.City, address.Country, address.PostalCode);

        // 4. Build PaymentInfo from the Stripe reference passed in by the controller
        var payment = PaymentInfo.Create(
            command.PaymentReference,
            command.PaymentMethod,
            subtotal - discountAmount + command.ShippingCost + command.TaxAmount,
            DateTime.UtcNow);

        // 5. Place the order — domain enforces all invariants
        var order = Order.Place(
            userId,
            shippingAddress,
            orderItems,
            command.ShippingCost,
            command.TaxAmount,
            payment,
            discountAmount,
            promoCodeId);

        // 6. If promo code used, record its usage (mutates PromoCode aggregate)
        if (promoCode is not null)
        {
            promoCode.RecordUsage();
            await _promoCodes.UpdateAsync(promoCode, ct);
        }

        await _orders.AddAsync(order, ct);
        await _uow.SaveChangesAsync(ct);
        // ↑ SaveChangesAsync dispatches: OrderPlacedEvent → ReduceStockHandler, ClearCartHandler, SendEmailHandler

        return Result<OrderDto>.Ok(order.ToDto());
    }
}
```

### After SaveChangesAsync: the event cascade

`OrderPlacedEvent` dispatches to:
1. `ReduceStockOnOrderPlacedHandler` (Inventory.Application) — reduces stock per item
2. `ClearCartOnOrderPlacedHandler` (Shopping.Application) — clears the user's cart
3. `SendOrderConfirmationOnOrderPlacedHandler` (Ordering.Application) — sends email
4. (Optionally) analytics handlers

This is why event handlers MUST catch exceptions (Rule 17). If the email handler fails, the order is already saved — that's correct. The email can be retried. The order must not be rolled back because an email failed.

**Tester handoff after Step 2:** Once `PlaceOrderCommand` handler is delivered, the tester writes handler unit tests in `ECommerce.Ordering.Tests/Handlers/`. See `.ai/plans/ddd-cqrs-migration/testing/tester-prompt-template.md` → Prompt 3.

---

## Step 3: Wire Up All Event Handler Stubs

In previous phases, you left stubs:

```csharp
// Inventory.Application/EventHandlers/ReduceStockOnOrderPlacedHandler.cs
public class ReduceStockOnOrderPlacedHandler : INotificationHandler<OrderPlacedEvent>
{
    private readonly IInventoryItemRepository _inventory;
    private readonly IUnitOfWork _uow;

    public async Task Handle(OrderPlacedEvent notification, CancellationToken ct)
    {
        try
        {
            foreach (var item in notification.Items)
            {
                var inventoryItem = await _inventory.GetByProductIdAsync(item.ProductId, ct);
                if (inventoryItem is null) continue;
                inventoryItem.Reduce(item.Quantity, $"Order {notification.OrderId}");
                await _inventory.UpdateAsync(inventoryItem, ct);
            }
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reduce stock for order {OrderId}", notification.OrderId);
        }
    }
}
```

```csharp
// Shopping.Application/EventHandlers/ClearCartOnOrderPlacedHandler.cs
public class ClearCartOnOrderPlacedHandler : INotificationHandler<OrderPlacedEvent>
{
    public async Task Handle(OrderPlacedEvent notification, CancellationToken ct)
    {
        try
        {
            var cart = await _carts.GetByUserIdAsync(notification.UserId, ct);
            if (cart is null) return;
            cart.Clear();
            await _carts.UpdateAsync(cart, ct);
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cart for user {UserId}", notification.UserId);
        }
    }
}
```

---

## Definition of Done

Full testing guide: `.ai/plans/ddd-cqrs-migration/testing/README.md`

**Characterization (integration — slow):**
- [ ] Characterization tests written and PASSING against OLD service (before any migration)
- [ ] Characterization tests still PASSING after cutover to new handlers

**Domain unit tests (fast — written after Step 1):**
- [ ] `ECommerce.Ordering.Tests/Domain/OrderTests.cs` written and PASSING
- Covers: Place/Confirm/Ship/Deliver/Cancel state transitions, invalid transitions throw, OrderItem snapshot, PaymentInfo validation

**Handler unit tests (fast — written after Step 2):**
- [ ] `ECommerce.Ordering.Tests/Handlers/` tests written and PASSING
- Covers: PlaceOrderCommand orchestration (cart check, promo calc, snapshot), event handler stubs

**Code:**
- [ ] `OrderStatus` Enumeration class with `CanTransitionTo()`
- [ ] `Order` aggregate with `Place`, `Confirm`, `Ship`, `Deliver`, `Cancel` (all via `TransitionTo`)
- [ ] `OrderItem` as immutable snapshot (name + price copied at order time)
- [ ] `PaymentInfo` value object (records Stripe reference — no payment logic)
- [ ] `PlaceOrderCommand` handler: cart validation, promo calculation, snapshot, domain call
- [ ] All Phase 3–6 event handler stubs implemented: ReduceStock, ClearCart, MarkVerified
- [ ] `OrderDeliveredEvent` wires to `MarkReviewVerifiedOnOrderDeliveredHandler`
- [ ] Old `OrderService` deleted after all characterization tests pass
- [ ] Full end-to-end integration test: place order → stock reduced → cart cleared → email sent

## What You Learned in Phase 7

- State machines in value objects: `CanTransitionTo()` keeps transition logic in the domain, not in handlers
- Snapshot pattern: OrderItem copies product data at order time — historical orders don't change when catalog changes
- Payment as external system: domain records result, doesn't process payment
- Cross-context orchestration: `PlaceOrderCommand` reads from 3+ contexts via shared DB
- Event cascade: one aggregate event triggers multiple independent handlers across contexts
- Why event handlers must swallow exceptions: partial failure (email failed) must not roll back the order
