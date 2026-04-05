using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;
using ECommerce.Ordering.Domain.Errors;
using ECommerce.Ordering.Domain.Events;
using ECommerce.Ordering.Domain.ValueObjects;

namespace ECommerce.Ordering.Domain.Aggregates.Order;

public sealed record OrderItemData(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, string? ImageUrl);

public sealed class Order : AggregateRoot
{
    public string OrderNumber { get; private set; } = null!;
    public Guid UserId { get; private set; }
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

    public static Result<Order> Place(
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
            return Result<Order>.Fail(OrderingErrors.OrderEmpty);

        decimal subtotal = items.Sum(i => i.UnitPrice * i.Quantity);
        decimal total = subtotal + shippingCost + taxAmount - discountAmount;

        if (total <= 0)
            return Result<Order>.Fail(OrderingErrors.OrderTotalInvalid);

        var order = new Order
        {
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
        };

        foreach (var item in items)
            order._items.Add(OrderItem.Create(Guid.NewGuid(), order.Id, item.ProductId, item.ProductName, item.UnitPrice, item.Quantity, item.ImageUrl));

        order.AddDomainEvent(new OrderPlacedEvent(order.Id, userId, order.Total, items));
        return Result<Order>.Ok(order);
    }

    public Result Confirm()
    {
        var result = TransitionTo(OrderStatus.Confirmed);
        if (!result.IsSuccess) return result;
        AddDomainEvent(new OrderConfirmedEvent(Id, UserId));
        return Result.Ok();
    }

    public Result Ship(string trackingNumber)
    {
        var result = TransitionTo(OrderStatus.Shipped);
        if (!result.IsSuccess) return result;
        AddDomainEvent(new OrderShippedEvent(Id, UserId, trackingNumber));
        return Result.Ok();
    }

    public Result Deliver()
    {
        var result = TransitionTo(OrderStatus.Delivered);
        if (!result.IsSuccess) return result;
        AddDomainEvent(new OrderDeliveredEvent(Id, UserId, _items.Select(i => i.ProductId).ToList()));
        return Result.Ok();
    }

    public Result Cancel(string reason)
    {
        var result = TransitionTo(OrderStatus.Cancelled);
        if (!result.IsSuccess) return result;
        AddDomainEvent(new OrderCancelledEvent(Id, UserId, reason));
        return Result.Ok();
    }

    private Result TransitionTo(OrderStatus newStatus)
    {
        if (!Status.CanTransitionTo(newStatus))
            return Result.Fail(OrderingErrors.OrderInvalidTransition);
        Status = newStatus;
        return Result.Ok();
    }

    private static string GenerateOrderNumber()
        => $"ORD-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
