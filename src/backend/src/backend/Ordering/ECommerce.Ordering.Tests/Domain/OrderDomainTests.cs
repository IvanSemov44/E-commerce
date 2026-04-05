using ECommerce.Ordering.Domain.Aggregates.Order;
using ECommerce.Ordering.Domain.Enums;
using ECommerce.Ordering.Domain.ValueObjects;
using ECommerce.SharedKernel.Results;
using Xunit;

namespace ECommerce.Ordering.Tests.Domain;

public class OrderStatusTests
{
    [Fact]
    public void OrderStatus_HasAllRequiredStatuses()
    {
        Assert.True(OrderStatus.Pending.Value == "Pending");
        Assert.True(OrderStatus.Confirmed.Value == "Confirmed");
        Assert.True(OrderStatus.Shipped.Value == "Shipped");
        Assert.True(OrderStatus.Delivered.Value == "Delivered");
        Assert.True(OrderStatus.Cancelled.Value == "Cancelled");
    }
}

public class PaymentInfoTests
{
    [Fact]
    public void Create_ValidPayment_Succeeds()
    {
        var result = PaymentInfo.Create("REF123", "credit_card", 100m, DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("REF123", result.Data.Reference);
    }

    [Fact]
    public void Create_NegativeAmount_Fails()
    {
        var result = PaymentInfo.Create("REF123", "credit_card", -50m, DateTime.UtcNow);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_ZeroAmount_Fails()
    {
        var result = PaymentInfo.Create("REF123", "credit_card", 0m, DateTime.UtcNow);

        Assert.False(result.IsSuccess);
    }
}

public class ShippingAddressTests
{
    [Fact]
    public void Create_ValidAddress_Succeeds()
    {
        var result = ShippingAddress.Create("123 Main St", "New York", "USA", "10001");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("123 Main St", result.Data.Street);
        Assert.Equal("New York", result.Data.City);
    }

    [Fact]
    public void Create_MissingStreet_Fails()
    {
        var result = ShippingAddress.Create("", "New York", "USA", "10001");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_MissingCity_Fails()
    {
        var result = ShippingAddress.Create("123 Main St", "", "USA", "10001");

        Assert.False(result.IsSuccess);
    }
}

public class OrderPlacementTests
{
    private static Order BuildOrder(
        Guid? userId = null,
        List<OrderItemData>? items = null,
        decimal subtotal = 100,
        decimal tax = 10,
        decimal shipping = 5)
    {
        userId ??= Guid.NewGuid();

        items ??= new List<OrderItemData>
        {
            new(Guid.NewGuid(), "Test Product", 50m, 2, null)
        };

        var address = ShippingAddress.Create("123 Main St", "NYC", "USA", "10001").Data!;
        var payment = PaymentInfo.Create("PAY123", "card", 100m, DateTime.UtcNow).Data!;

        var result = Order.Place(userId.Value, address, items, shipping, tax, payment);
        return result.Data!;
    }

    [Fact]
    public void Place_ValidOrder_Succeeds()
    {
        var items = new List<OrderItemData>
        {
            new(Guid.NewGuid(), "Product 1", 50m, 2, null)
        };
        var address = ShippingAddress.Create("123 Main St", "NYC", "USA", "10001").Data!;
        var payment = PaymentInfo.Create("PAY123", "card", 120m, DateTime.UtcNow).Data!;

        var result = Order.Place(Guid.NewGuid(), address, items, 5m, 10m, payment);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(OrderStatus.Pending, result.Data.Status);
        Assert.Equal(2, result.Data.Items.Count);
    }

    [Fact]
    public void Place_EmptyItems_Fails()
    {
        var address = ShippingAddress.Create("123 Main St", "NYC", "USA", "10001").Data!;
        var payment = PaymentInfo.Create("PAY123", "card", 15m, DateTime.UtcNow).Data!;

        var result = Order.Place(Guid.NewGuid(), address, new List<OrderItemData>(), 5m, 10m, payment);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Confirm_PendingOrder_TransitionsToConfirmed()
    {
        var order = BuildOrder();

        var result = order.Confirm();

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Confirm_AlreadyConfirmed_Fails()
    {
        var order = BuildOrder();
        order.Confirm();

        var result = order.Confirm();

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Ship_ConfirmedOrder_TransitionsToShipped()
    {
        var order = BuildOrder();
        order.Confirm();

        var result = order.Ship("TRK123456");

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.Equal("TRK123456", order.TrackingNumber);
    }

    [Fact]
    public void Ship_PendingOrder_Fails()
    {
        var order = BuildOrder();

        var result = order.Ship("TRK123456");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Cancel_PendingOrder_Succeeds()
    {
        var order = BuildOrder();

        var result = order.Cancel("Customer requested");

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("Customer requested", order.CancellationReason);
    }

    [Fact]
    public void Cancel_ShippedOrder_Fails()
    {
        var order = BuildOrder();
        order.Confirm();
        order.Ship("TRK123456");

        var result = order.Cancel("Too late");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Cancel_DeliveredOrder_Fails()
    {
        var order = BuildOrder();
        order.Confirm();
        order.Ship("TRK123456");
        order.Deliver();

        var result = order.Cancel("Way too late");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Deliver_ShippedOrder_TransitionsToDelivered()
    {
        var order = BuildOrder();
        order.Confirm();
        order.Ship("TRK123456");

        order.Deliver();

        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void TotalCalculation_IncludesSubtotalTaxAndShipping()
    {
        var items = new List<OrderItemData>
        {
            new(Guid.NewGuid(), "Product", 100m, 1, null)
        };
        var address = ShippingAddress.Create("123 Main St", "NYC", "USA", "10001").Data!;
        var payment = PaymentInfo.Create("PAY123", "card", 115m, DateTime.UtcNow).Data!;

        var order = Order.Place(Guid.NewGuid(), address, items, 10m, 5m, payment).Data!;

        Assert.Equal(100m, order.Subtotal);
        Assert.Equal(5m, order.TaxAmount);
        Assert.Equal(10m, order.ShippingCost);
        Assert.Equal(115m, order.Total);
    }

    [Fact]
    public void OrderNumber_IsGenerated()
    {
        var order = BuildOrder();

        Assert.NotNull(order.OrderNumber);
        Assert.NotEmpty(order.OrderNumber);
    }

    [Fact]
    public void OrderItems_ArePreserved()
    {
        var items = new List<OrderItemData>
        {
            new(Guid.NewGuid(), "Product 1", 50m, 2, null),
            new(Guid.NewGuid(), "Product 2", 30m, 1, null)
        };
        var address = ShippingAddress.Create("123 Main St", "NYC", "USA", "10001").Data!;
        var payment = PaymentInfo.Create("PAY123", "card", 130m, DateTime.UtcNow).Data!;

        var order = Order.Place(Guid.NewGuid(), address, items, 10m, 0m, payment).Data!;

        Assert.Equal(2, order.Items.Count);
        Assert.Equal(2, order.Items.First().Quantity);
        Assert.Equal(1, order.Items.Skip(1).First().Quantity);
    }
}
