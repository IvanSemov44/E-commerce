using ECommerce.Ordering.Domain.Aggregates.Order;
using ECommerce.Ordering.Domain.Errors;
using ECommerce.Ordering.Domain.ValueObjects;
using ECommerce.SharedKernel.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Ordering.Tests.Domain;

[TestClass]
public class PaymentInfoTests
{
    [TestMethod]
    public void Create_EmptyReference_ReturnsFailure()
    {
        var result = PaymentInfo.Create("", "credit_card", 100m, DateTime.UtcNow);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(OrderingErrors.PaymentRefEmpty.Code);
    }

    [TestMethod]
    public void Create_InvalidAmount_ReturnsFailure()
    {
        var result = PaymentInfo.Create("PAY123", "credit_card", 0m, DateTime.UtcNow);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(OrderingErrors.PaymentAmountInvalid.Code);
    }

    [TestMethod]
    public void Create_NegativeAmount_ReturnsFailure()
    {
        var result = PaymentInfo.Create("PAY123", "credit_card", -50m, DateTime.UtcNow);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(OrderingErrors.PaymentAmountInvalid.Code);
    }

    [TestMethod]
    public void Create_Valid_ReturnsSuccess()
    {
        var result = PaymentInfo.Create("PAY123", "credit_card", 100m, DateTime.UtcNow);

        result.IsSuccess.ShouldBeTrue();
        var payment = result.GetDataOrThrow();
        payment.PaymentReference.ShouldBe("PAY123");
        payment.PaymentMethod.ShouldBe("credit_card");
        payment.PaidAmount.ShouldBe(100m);
    }
}

[TestClass]
public class OrderStatusTests
{
    [TestMethod]
    public void Pending_CanTransitionToConfirmed()
    {
        OrderStatus.Pending.CanTransitionTo(OrderStatus.Confirmed).ShouldBeTrue();
    }

    [TestMethod]
    public void Pending_CanTransitionToCancelled()
    {
        OrderStatus.Pending.CanTransitionTo(OrderStatus.Cancelled).ShouldBeTrue();
    }

    [TestMethod]
    public void Pending_CannotTransitionToShipped()
    {
        OrderStatus.Pending.CanTransitionTo(OrderStatus.Shipped).ShouldBeFalse();
    }

    [TestMethod]
    public void Confirmed_CanTransitionToProcessing()
    {
        OrderStatus.Confirmed.CanTransitionTo(OrderStatus.Processing).ShouldBeTrue();
    }

    [TestMethod]
    public void Confirmed_CanTransitionToCancelled()
    {
        OrderStatus.Confirmed.CanTransitionTo(OrderStatus.Cancelled).ShouldBeTrue();
    }

    [TestMethod]
    public void Processing_CanTransitionToShipped()
    {
        OrderStatus.Processing.CanTransitionTo(OrderStatus.Shipped).ShouldBeTrue();
    }

    [TestMethod]
    public void Shipped_CanTransitionToDelivered()
    {
        OrderStatus.Shipped.CanTransitionTo(OrderStatus.Delivered).ShouldBeTrue();
    }

    [TestMethod]
    public void Delivered_CannotTransition()
    {
        OrderStatus.Delivered.CanTransitionTo(OrderStatus.Cancelled).ShouldBeFalse();
        OrderStatus.Delivered.CanTransitionTo(OrderStatus.Processing).ShouldBeFalse();
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

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(OrderingErrors.OrderEmpty.Code);
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

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(OrderingErrors.OrderTotalInvalid.Code);
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

        result.IsSuccess.ShouldBeTrue();
        var order = result.GetDataOrThrow();
        order.UserId.ShouldBe(userId);
        order.Status.ShouldBe(OrderStatus.Pending);
        order.Subtotal.ShouldBe(100m);
        order.ShippingCost.ShouldBe(10m);
        order.TaxAmount.ShouldBe(5m);
        order.Total.ShouldBe(115m);
        order.Items.Count.ShouldBe(1);
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

        result.IsSuccess.ShouldBeTrue();
        var order = result.GetDataOrThrow();
        order.DiscountAmount.ShouldBe(11m);
        order.Total.ShouldBe(104m); // 100 + 10 + 5 - 11
    }

    [TestMethod]
    public void Confirm_PendingOrder_Succeeds()
    {
        var order = CreateTestOrder();

        var result = order.Confirm();

        result.IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Confirmed);
    }

    [TestMethod]
    public void Confirm_AlreadyConfirmed_ReturnsFailure()
    {
        var order = CreateTestOrder();
        order.Confirm();

        var result = order.Confirm();

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(OrderingErrors.OrderInvalidTransition.Code);
    }

    [TestMethod]
    public void Cancel_PendingOrder_Succeeds()
    {
        var order = CreateTestOrder();

        var result = order.Cancel("Customer requested");

        result.IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Cancelled);
    }

    [TestMethod]
    public void Cancel_ConfirmedOrder_Succeeds()
    {
        var order = CreateTestOrder();
        order.Confirm();

        var result = order.Cancel("Changed mind");

        result.IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Cancelled);
    }

    [TestMethod]
    public void Items_AreReadOnly()
    {
        var order = CreateTestOrder();

        order.Items.Count.ShouldBe(1);
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
