# Phase 7, Step 6: Handler Tests

**Prerequisite**: Step 5 (Domain tests) complete and passing.

Write comprehensive tests for command and query handlers using fake in-memory repository.

---

## File: `src/backend/ECommerce.Tests/Application/Orders/OrdersHandlerTests.cs`

```csharp
using ECommerce.Orders.Application.Commands;
using ECommerce.Orders.Application.CommandHandlers;
using ECommerce.Orders.Application.DTOs;
using ECommerce.Orders.Application.Queries;
using ECommerce.Orders.Application.QueryHandlers;
using ECommerce.Orders.Domain.Aggregates.Order;
using ECommerce.Orders.Domain.Interfaces;
using ECommerce.Orders.Domain.ValueObjects;
using ECommerce.SharedKernel;
using Xunit;

namespace ECommerce.Tests.Application.Orders;

// Fake repository
public class FakeOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _orders = new();

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_orders.TryGetValue(id, out var o) ? o : null);

    public Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default)
        => Task.FromResult(_orders.Values.FirstOrDefault(o => o.OrderNumber.Value == orderNumber));

    public async Task<(List<Order> Items, int TotalCount)> GetByCustomerAsync(
        Guid customerId, int page, int pageSize, CancellationToken ct = default)
    {
        var items = _orders.Values
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var total = _orders.Values.Count(o => o.CustomerId == customerId);
        return (items, total);
    }

    public async Task<(List<Order> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? status, CancellationToken ct = default)
    {
        var query = _orders.Values.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status.ToString() == status);

        var total = query.Count();
        var items = query.OrderByDescending(o => o.CreatedAt)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

        return (items, total);
    }

    public async Task<(List<Order> Items, int TotalCount)> GetPendingAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var items = _orders.Values
            .Where(o => o.Status.ToString() == "Pending")
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (items, _orders.Values.Count(o => o.Status.ToString() == "Pending"));
    }

    public Task UpsertAsync(Order order, CancellationToken ct = default)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Order order, CancellationToken ct = default)
    {
        _orders.Remove(order.Id);
        return Task.CompletedTask;
    }

    public void Seed(Order order) => _orders[order.Id] = order;
}

public class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }
    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

public class PlaceOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidItems_CreatesOrderAndReturns201()
    {
        var repo = new FakeOrderRepository();
        var uow = new FakeUnitOfWork();
        var handler = new PlaceOrderCommandHandler(repo, uow);

        var cmd = new PlaceOrderCommand(
            Guid.NewGuid(),
            new List<OrderLineItemRequest>
            {
                new(Guid.NewGuid(), 2, 50),
                new(Guid.NewGuid(), 1, 100)
            },
            20,
            10);

        var result = await handler.Handle(cmd, default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.OrderNumber);
        Assert.Equal("Pending", result.Value!.Status);
        Assert.Equal(220, result.Value!.Total);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Handle_EmptyItems_ReturnsFailed()
    {
        var repo = new FakeOrderRepository();
        var uow = new FakeUnitOfWork();
        var handler = new PlaceOrderCommandHandler(repo, uow);

        var cmd = new PlaceOrderCommand(Guid.NewGuid(), new List<OrderLineItemRequest>(), 0, 0);

        var result = await handler.Handle(cmd, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("ORDER_EMPTY", result.Error!.Code);
    }
}

public class ConfirmOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidId_ConfirmsOrder()
    {
        var order = BuildOrder();
        var repo = new FakeOrderRepository();
        repo.Seed(order);
        var uow = new FakeUnitOfWork();
        var handler = new ConfirmOrderCommandHandler(repo, uow);

        var result = await handler.Handle(new ConfirmOrderCommand(order.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Confirmed", result.Value!.Status);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsFailed()
    {
        var repo = new FakeOrderRepository();
        var uow = new FakeUnitOfWork();
        var handler = new ConfirmOrderCommandHandler(repo, uow);

        var result = await handler.Handle(new ConfirmOrderCommand(Guid.NewGuid()), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("ORDER_NOT_FOUND", result.Error!.Code);
    }
}

public class ShipOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_ConfirmedOrder_ShipsWithTracking()
    {
        var order = BuildOrder();
        order.Confirm();

        var repo = new FakeOrderRepository();
        repo.Seed(order);
        var uow = new FakeUnitOfWork();
        var handler = new ShipOrderCommandHandler(repo, uow);

        var result = await handler.Handle(new ShipOrderCommand(order.Id, "TRK123"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Shipped", result.Value!.Status);
        Assert.Equal("TRK123", result.Value!.TrackingNumber);
    }
}

public class CancelOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_PendingOrder_Cancels()
    {
        var order = BuildOrder();
        var repo = new FakeOrderRepository();
        repo.Seed(order);
        var uow = new FakeUnitOfWork();
        var handler = new CancelOrderCommandHandler(repo, uow);

        var result = await handler.Handle(new CancelOrderCommand(order.Id, "Changed mind"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Cancelled", result.Value!.Status);
    }

    [Fact]
    public async Task Handle_ShippedOrder_ReturnsFailed()
    {
        var order = BuildOrder();
        order.Confirm();
        order.Ship("TRK123");

        var repo = new FakeOrderRepository();
        repo.Seed(order);
        var uow = new FakeUnitOfWork();
        var handler = new CancelOrderCommandHandler(repo, uow);

        var result = await handler.Handle(new CancelOrderCommand(order.Id, "Too late"), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("ORDER_CANNOT_CANCEL_SHIPPED", result.Error!.Code);
    }
}

public class GetCustomerOrdersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPaginatedOrders()
    {
        var customerId = Guid.NewGuid();
        var order = BuildOrder(customerId);

        var repo = new FakeOrderRepository();
        repo.Seed(order);

        var handler = new GetCustomerOrdersQueryHandler(repo);
        var result = await handler.Handle(new GetCustomerOrdersQuery(customerId, 1, 10), default);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(customerId, result.Value.Items[0].CustomerId);
    }
}

public class GetPendingOrdersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPendingOnly()
    {
        var pending = BuildOrder();
        var confirmed = BuildOrder();
        confirmed.Confirm();

        var repo = new FakeOrderRepository();
        repo.Seed(pending);
        repo.Seed(confirmed);

        var handler = new GetPendingOrdersQueryHandler(repo);
        var result = await handler.Handle(new GetPendingOrdersQuery(1, 10), default);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("Pending", result.Value.Items[0].Status);
    }
}

private static Order BuildOrder(Guid? customerId = null)
{
    var items = new List<OrderLineItem>
    {
        new(Guid.NewGuid(), Quantity.Create(1).Value!, Money.Create(100).Value!)
    };

    return Order.Create(
        customerId ?? Guid.NewGuid(),
        OrderNumber.Create("ORD-20260404-123456").Value!,
        items,
        Money.Create(100).Value!,
        Money.Create(10).Value!,
        Money.Create(5).Value!).Value!;
}
```

---

## Acceptance Criteria

- [ ] All command handler tests pass
- [ ] All query handler tests pass
- [ ] Duplicate order check works (if implemented)
- [ ] Empty order validation works
- [ ] Invalid quantity/price validation works
- [ ] Status transitions work correctly
- [ ] Cannot ship pending orders
- [ ] Cannot cancel shipped orders
- [ ] Query filtering by status works
- [ ] Pagination works correctly
