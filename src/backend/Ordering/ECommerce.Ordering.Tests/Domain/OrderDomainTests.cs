using ECommerce.Ordering.Domain.Aggregates.Order;
using ECommerce.Ordering.Domain.Errors;
using ECommerce.Ordering.Domain.ValueObjects;
using ECommerce.SharedKernel.Results;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Ordering.Tests.Domain;

[TestClass]
public class PaymentInfoTests
{
    [TestMethod]
    public void Create_EmptyReference_ReturnsFailure()
    {
        var result = PaymentInfo.Create("", "credit_card", 100m, DateTime.UtcNow);

        result.IsSuccess.Should().BeFalse();
        result.GetErrorOrThrow().Code.Should().Be(OrderingErrors.PaymentRefEmpty.Code);
    }

    [TestMethod]
    public void Create_InvalidAmount_ReturnsFailure()
    {
        var result = PaymentInfo.Create("PAY123", "credit_card", 0m, DateTime.UtcNow);

        result.IsSuccess.Should().BeFalse();
        result.GetErrorOrThrow().Code.Should().Be(OrderingErrors.PaymentAmountInvalid.Code);
    }

    [TestMethod]
    public void Create_NegativeAmount_ReturnsFailure()
    {
        var result = PaymentInfo.Create("PAY123", "credit_card", -50m, DateTime.UtcNow);

        result.IsSuccess.Should().BeFalse();
        result.GetErrorOrThrow().Code.Should().Be(OrderingErrors.PaymentAmountInvalid.Code);
    }

    [TestMethod]
    public void Create_Valid_ReturnsSuccess()
    {
        var result = PaymentInfo.Create("PAY123", "credit_card", 100m, DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        var payment = result.GetDataOrThrow();
        payment.PaymentReference.Should().Be("PAY123");
        payment.PaymentMethod.Should().Be("credit_card");
        payment.PaidAmount.Should().Be(100m);
    }
}

[TestClass]
public class OrderStatusTests
{
    [TestMethod]
    public void Pending_CanTransitionToConfirmed()
    {
        OrderStatus.Pending.CanTransitionTo(OrderStatus.Confirmed).Should().BeTrue();
    }

    [TestMethod]
    public void Pending_CanTransitionToCancelled()
    {
        OrderStatus.Pending.CanTransitionTo(OrderStatus.Cancelled).Should().BeTrue();
    }

    [TestMethod]
    public void Pending_CannotTransitionToShipped()
    {
        OrderStatus.Pending.CanTransitionTo(OrderStatus.Shipped).Should().BeFalse();
    }

    [TestMethod]
    public void Confirmed_CanTransitionToProcessing()
    {
        OrderStatus.Confirmed.CanTransitionTo(OrderStatus.Processing).Should().BeTrue();
    }

    [TestMethod]
    public void Confirmed_CanTransitionToCancelled()
    {
        OrderStatus.Confirmed.CanTransitionTo(OrderStatus.Cancelled).Should().BeTrue();
    }

    [TestMethod]
    public void Processing_CanTransitionToShipped()
    {
        OrderStatus.Processing.CanTransitionTo(OrderStatus.Shipped).Should().BeTrue();
    }

    [TestMethod]
    public void Shipped_CanTransitionToDelivered()
    {
        OrderStatus.Shipped.CanTransitionTo(OrderStatus.Delivered).Should().BeTrue();
    }

    [TestMethod]
    public void Delivered_CannotTransition()
    {
        OrderStatus.Delivered.CanTransitionTo(OrderStatus.Cancelled).Should().BeFalse();
        OrderStatus.Delivered.CanTransitionTo(OrderStatus.Processing).Should().BeFalse();
    }
}

[TestClass]
public class OrderTests
{
    [TestMethod]
    public void Place_EmptyItems_ReturnsFailure()
    {
        var address = ShippingAddress.Create("123 Main St", "NYC", "USA", "10001");
        var items = new List<OrderItemData>();
        var payment = PaymentInfo.Create("PAY123", "card", 100m, DateTime.UtcNow).GetDataOrThrow();

        var result = Order.Place(Guid.NewGuid(), address, items, 10m, 5m, payment);

        result.IsSuccess.Should().BeFalse();
        result.GetErrorOrThrow().Code.Should().Be(OrderingErrors.OrderEmpty.Code);
    }

    [TestMethod]
    public void Place_NegativeTotal_ReturnsFailure()
    {
        var address = ShippingAddress.Create("123 Main St", "NYC", "USA", "10001");
        var items = new List<OrderItemData>
        {
            new(Guid.NewGuid(), "Product", 100m, 1, null)
        };
        var payment = PaymentInfo.Create("PAY123", "card", 50m, DateTime.UtcNow).GetDataOrThrow();

        var result = Order.Place(Guid.NewGuid(), address, items, 10m, 5m, payment, discountAmount: 200m);

        result.IsSuccess.Should().BeFalse();
        result.GetErrorOrThrow().Code.Should().Be(OrderingErrors.OrderTotalInvalid.Code);
    }

    [TestMethod]
    public void Place_Valid_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var address = ShippingAddress.Create("123 Main St", "NYC", "USA", "10001");
        var items = new List<OrderItemData>
        {
            new(Guid.NewGuid(), "Product", 100m, 1, null)
        };
        var payment = PaymentInfo.Create("PAY123", "card", 115m, DateTime.UtcNow).GetDataOrThrow();

        var result = Order.Place(userId, address, items, 10m, 5m, payment);

        result.IsSuccess.Should().BeTrue();
        var order = result.GetDataOrThrow();
        order.UserId.Should().Be(userId);
        order.Status.Should().Be(OrderStatus.Pending);
        order.Subtotal.Should().Be(100m);
        order.ShippingCost.Should().Be(10m);
        order.TaxAmount.Should().Be(5m);
        order.Total.Should().Be(115m);
        order.Items.Should().HaveCount(1);
    }

    [TestMethod]
    public void Place_WithDiscount_CalculatesCorrectTotal()
    {
        var address = ShippingAddress.Create("123 Main St", "NYC", "USA", "10001");
        var items = new List<OrderItemData>
        {
            new(Guid.NewGuid(), "Product", 100m, 1, null)
        };
        var payment = PaymentInfo.Create("PAY123", "card", 104m, DateTime.UtcNow).GetDataOrThrow();

        var result = Order.Place(
            Guid.NewGuid(), address, items, 10m, 5m, payment, discountAmount: 11m);

        result.IsSuccess.Should().BeTrue();
        var order = result.GetDataOrThrow();
        order.DiscountAmount.Should().Be(11m);
        order.Total.Should().Be(104m); // 100 + 10 + 5 - 11
    }

    [TestMethod]
    public void Confirm_PendingOrder_Succeeds()
    {
        var order = CreateTestOrder();

        var result = order.Confirm();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [TestMethod]
    public void Confirm_AlreadyConfirmed_ReturnsFailure()
    {
        var order = CreateTestOrder();
        order.Confirm();

        var result = order.Confirm();

        result.IsSuccess.Should().BeFalse();
        result.GetErrorOrThrow().Code.Should().Be(OrderingErrors.OrderInvalidTransition.Code);
    }

    [TestMethod]
    public void Cancel_PendingOrder_Succeeds()
    {
        var order = CreateTestOrder();

        var result = order.Cancel("Customer requested");

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [TestMethod]
    public void Cancel_ConfirmedOrder_Succeeds()
    {
        var order = CreateTestOrder();
        order.Confirm();

        var result = order.Cancel("Changed mind");

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [TestMethod]
    public void Items_AreReadOnly()
    {
        var order = CreateTestOrder();

        order.Items.Should().HaveCount(1);
    }

    private static Order CreateTestOrder()
    {
        var items = new List<OrderItemData>
        {
            new(Guid.NewGuid(), "Test Product", 100m, 1, null)
        };
        var address = ShippingAddress.Create("123 Main St", "NYC", "USA", "10001");
        var payment = PaymentInfo.Create("PAY123", "card", 115m, DateTime.UtcNow).GetDataOrThrow();

        return Order.Place(Guid.NewGuid(), address, items, 10m, 5m, payment).GetDataOrThrow();
    }
}
