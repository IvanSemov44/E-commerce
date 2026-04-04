# Phase 7, Step 5: Domain Tests

**Prerequisite**: Step 4 (Cutover) complete and compiled.

Write comprehensive unit tests for the Orders domain layer.

---

## File: `src/backend/ECommerce.Tests/Domain/Orders/OrdersDomainTests.cs`

```csharp
using ECommerce.Orders.Domain;
using ECommerce.Orders.Domain.Aggregates.Order;
using ECommerce.Orders.Domain.Enums;
using ECommerce.Orders.Domain.ValueObjects;
using ECommerce.SharedKernel;
using Xunit;

namespace ECommerce.Tests.Domain.Orders;

public class OrderNumberTests
{
    [Fact]
    public void Create_ValidFormat_ReturnsSuccess()
    {
        var result = OrderNumber.Create("ORD-20260404-123456");
        Assert.True(result.IsSuccess);
        Assert.Equal("ORD-20260404-123456", result.Value!.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("INVALID")]
    [InlineData("ORD-2026-123")]
    public void Create_InvalidFormat_ReturnsFailed(string value)
    {
        var result = OrderNumber.Create(value);
        Assert.False(result.IsSuccess);
        Assert.Equal(OrdersErrors.OrderNumberInvalid.Code, result.Error!.Code);
    }
}

public class MoneyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(99.99)]
    [InlineData(1000.00)]
    public void Create_ValidAmount_ReturnsSuccess(decimal amount)
    {
        var result = Money.Create(amount);
        Assert.True(result.IsSuccess);
        Assert.Equal(amount, result.Value!.Amount);
    }

    [Fact]
    public void Create_NegativeAmount_ReturnsFailed()
    {
        var result = Money.Create(-10);
        Assert.False(result.IsSuccess);
        Assert.Equal(OrdersErrors.OrderInvalidPrice.Code, result.Error!.Code);
    }

    [Fact]
    public void Addition_Works()
    {
        var m1 = Money.Create(10).Value!;
        var m2 = Money.Create(20).Value!;
        var result = m1 + m2;
        Assert.Equal(30, result.Amount);
    }
}

public class QuantityTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Create_PositiveQuantity_ReturnsSuccess(int qty)
    {
        var result = Quantity.Create(qty);
        Assert.True(result.IsSuccess);
        Assert.Equal(qty, result.Value!.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_InvalidQuantity_ReturnsFailed(int qty)
    {
        var result = Quantity.Create(qty);
        Assert.False(result.IsSuccess);
        Assert.Equal(OrdersErrors.OrderInvalidQuantity.Code, result.Error!.Code);
    }
}

public class OrderTests
{
    private static Order BuildOrder(
        List<OrderLineItem>? items = null,
        decimal subtotal = 100,
        decimal tax = 10,
        decimal shipping = 5)
    {
        if (items is null)
        {
            items = new List<OrderLineItem>
            {
                new(Guid.NewGuid(), Quantity.Create(2).Value!, Money.Create(50).Value!)
            };
        }

        var result = Order.Create(
            Guid.NewGuid(),
            OrderNumber.Create("ORD-20260404-123456").Value!,
            items,
            Money.Create(subtotal).Value!,
            Money.Create(tax).Value!,
            Money.Create(shipping).Value!);

        return result.Value!;
    }

    [Fact]
    public void Create_ValidData_RaisesOrderPlacedEvent()
    {
        var customerId = Guid.NewGuid();
        var items = new List<OrderLineItem>
        {
            new(Guid.NewGuid(), Quantity.Create(1).Value!, Money.Create(100).Value!)
        };

        var result = Order.Create(
            customerId,
            OrderNumber.Create("ORD-20260404-123456").Value!,
            items,
            Money.Create(100).Value!,
            Money.Create(10).Value!,
            Money.Create(5).Value!);

        Assert.True(result.IsSuccess);
        var order = result.Value!;
        Assert.Single(order.DomainEvents);
        var evt = order.DomainEvents.First() as OrderPlacedEvent;
        Assert.NotNull(evt);
        Assert.Equal(customerId, evt!.CustomerId);
        Assert.Equal(115, evt.TotalAmount);
    }

    [Fact]
    public void Create_EmptyItems_ReturnsFailed()
    {
        var result = Order.Create(
            Guid.NewGuid(),
            OrderNumber.Create("ORD-20260404-123456").Value!,
            new List<OrderLineItem>(),
            Money.Create(0).Value!,
            Money.Create(0).Value!,
            Money.Create(0).Value!);

        Assert.False(result.IsSuccess);
        Assert.Equal(OrdersErrors.OrderEmpty.Code, result.Error!.Code);
    }

    [Fact]
    public void Create_NewOrderStatus_IsPending()
    {
        var order = BuildOrder();
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void Total_CalculatedCorrectly()
    {
        var order = BuildOrder(subtotal: 100, tax: 10, shipping: 5);
        Assert.Equal(115, order.Total.Amount);
    }

    [Fact]
    public void Confirm_PendingOrder_SucceedsAndRaisesEvent()
    {
        var order = BuildOrder();
        var result = order.Confirm();

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        var evt = order.DomainEvents.OfType<OrderConfirmedEvent>().FirstOrDefault();
        Assert.NotNull(evt);
    }

    [Fact]
    public void Confirm_AlreadyConfirmed_ReturnsFailed()
    {
        var order = BuildOrder();
        order.Confirm();

        var result = order.Confirm();
        Assert.False(result.IsSuccess);
        Assert.Equal(OrdersErrors.OrderAlreadyConfirmed.Code, result.Error!.Code);
    }

    [Fact]
    public void Ship_ConfirmedOrder_SucceedsAndRaisesEvent()
    {
        var order = BuildOrder();
        order.Confirm();

        var result = order.Ship("TRK123");
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.Equal("TRK123", order.TrackingNumber);
        var evt = order.DomainEvents.OfType<OrderShippedEvent>().FirstOrDefault();
        Assert.NotNull(evt);
    }

    [Fact]
    public void Ship_PendingOrder_ReturnsFailed()
    {
        var order = BuildOrder();

        var result = order.Ship("TRK123");
        Assert.False(result.IsSuccess);
        Assert.Equal(OrdersErrors.OrderCannotShipPending.Code, result.Error!.Code);
    }

    [Fact]
    public void Cancel_PendingOrder_Succeeds()
    {
        var order = BuildOrder();

        var result = order.Cancel("Changed mind");
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("Changed mind", order.CancellationReason);
    }

    [Fact]
    public void Cancel_ShippedOrder_ReturnsFailed()
    {
        var order = BuildOrder();
        order.Confirm();
        order.Ship("TRK123");

        var result = order.Cancel("Too late");
        Assert.False(result.IsSuccess);
        Assert.Equal(OrdersErrors.OrderCannotCancelShipped.Code, result.Error!.Code);
    }

    [Fact]
    public void Deliver_ChangesStatusToDelivered()
    {
        var order = BuildOrder();
        order.Confirm();
        order.Ship("TRK123");

        order.Deliver();
        Assert.Equal(OrderStatus.Delivered, order.Status);
        var evt = order.DomainEvents.OfType<OrderDeliveredEvent>().FirstOrDefault();
        Assert.NotNull(evt);
    }

    [Fact]
    public void Cancel_DeliveredOrder_ReturnsFailed()
    {
        var order = BuildOrder();
        order.Confirm();
        order.Ship("TRK123");
        order.Deliver();

        var result = order.Cancel("Too late");
        Assert.False(result.IsSuccess);
        Assert.Equal(OrdersErrors.OrderCannotCancelDelivered.Code, result.Error!.Code);
    }
}
```

---

## Acceptance Criteria

- [ ] OrderNumber validation tests pass
- [ ] Money value object tests pass (non-negative, addition)
- [ ] Quantity validation tests pass
- [ ] Order creation with valid data succeeds and raises event
- [ ] Empty order creation fails with ORDER_EMPTY
- [ ] Status transitions work: Pending → Confirmed → Shipped → Delivered
- [ ] Cannot confirm already-confirmed order
- [ ] Cannot ship pending order
- [ ] Cannot cancel shipped or delivered orders
- [ ] Total calculation correct (subtotal + tax + shipping)
- [ ] All domain events raised correctly
