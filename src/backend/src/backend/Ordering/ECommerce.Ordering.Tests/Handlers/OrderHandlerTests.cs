using ECommerce.Ordering.Application.Commands.PlaceOrder;
using ECommerce.Ordering.Application.Commands.ConfirmOrder;
using ECommerce.Ordering.Application.Commands.ShipOrder;
using ECommerce.Ordering.Application.Commands.CancelOrder;
using ECommerce.Ordering.Application.Queries.GetOrderById;
using ECommerce.Ordering.Application.CommandHandlers;
using ECommerce.Ordering.Application.QueryHandlers;
using ECommerce.Ordering.Domain.Aggregates.Order;
using ECommerce.Ordering.Domain.Interfaces;
using ECommerce.Ordering.Domain.ValueObjects;
using ECommerce.SharedKernel;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using Moq;
using Xunit;

namespace ECommerce.Ordering.Tests.Handlers;

// Fake implementations for testing
public class FakeOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _orders = new();

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_orders.TryGetValue(id, out var o) ? o : null);

    public Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default)
        => Task.FromResult(_orders.Values.FirstOrDefault(o => o.OrderNumber == orderNumber));

    public async Task<(List<Order> Items, int TotalCount)> GetByCustomerAsync(
        Guid customerId, int page, int pageSize, CancellationToken ct = default)
    {
        var items = _orders.Values
            .Where(o => o.UserId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var total = _orders.Values.Count(o => o.UserId == customerId);
        return (items, total);
    }

    public async Task<(List<Order> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? status, CancellationToken ct = default)
    {
        var query = _orders.Values.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status.Value == status);

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
            .Where(o => o.Status.Value == "Pending")
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (items, _orders.Values.Count(o => o.Status.Value == "Pending"));
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
    public async Task Handle_ValidCommand_CreatesOrderAndReturnsSuccess()
    {
        // Arrange
        var repo = new FakeOrderRepository();
        var uow = new FakeUnitOfWork();
        var dbReaderMock = new Mock<IOrderingDbReader>();
        var currentUserMock = new Mock<IOrderingCurrentUserService>();

        currentUserMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        dbReaderMock.Setup(x => x.GetProductsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductData>
            {
                new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Test Product", 50m, null)
            });
        dbReaderMock.Setup(x => x.GetShippingAddressAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddressData("123 Main", "NYC", "USA", "10001"));
        dbReaderMock.Setup(x => x.GetPromoCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductData?)null);

        var handler = new PlaceOrderCommandHandler(repo, uow, dbReaderMock.Object, currentUserMock.Object);
        var cmd = new PlaceOrderCommand
        {
            UserId = Guid.NewGuid(),
            ShippingAddressId = Guid.NewGuid(),
            CartItems = new List<CartItemInput>
            {
                new(Guid.Parse("22222222-2222-2222-2222-222222222222"), 2)
            },
            PaymentMethod = "card",
            PaymentReference = "REF123",
            ShippingCost = 10m,
            TaxAmount = 5m
        };

        // Act
        var result = await handler.Handle(cmd, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, uow.SaveCount);
    }
}

public class ConfirmOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_PendingOrder_ConfirmsSuccessfully()
    {
        // Arrange
        var order = CreateTestOrder();
        var repo = new FakeOrderRepository();
        repo.Seed(order);
        var uow = new FakeUnitOfWork();

        var handler = new ConfirmOrderCommandHandler(repo, uow);
        var cmd = new ConfirmOrderCommand(order.Id);

        // Act
        var result = await handler.Handle(cmd, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Confirmed", result.Data.Status);
    }

    [Fact]
    public async Task Handle_UnknownOrder_ReturnsFailed()
    {
        // Arrange
        var repo = new FakeOrderRepository();
        var uow = new FakeUnitOfWork();
        var handler = new ConfirmOrderCommandHandler(repo, uow);
        var cmd = new ConfirmOrderCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(cmd, default);

        // Assert
        Assert.False(result.IsSuccess);
    }
}

public class ShipOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_ConfirmedOrder_ShipsWithTracking()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Confirm();

        var repo = new FakeOrderRepository();
        repo.Seed(order);
        var uow = new FakeUnitOfWork();

        var handler = new ShipOrderCommandHandler(repo, uow);
        var cmd = new ShipOrderCommand(order.Id, "TRK123456");

        // Act
        var result = await handler.Handle(cmd, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Shipped", result.Data.Status);
        Assert.Equal("TRK123456", result.Data.TrackingNumber);
    }

    [Fact]
    public async Task Handle_PendingOrder_ReturnsFailed()
    {
        // Arrange
        var order = CreateTestOrder();
        var repo = new FakeOrderRepository();
        repo.Seed(order);
        var uow = new FakeUnitOfWork();

        var handler = new ShipOrderCommandHandler(repo, uow);
        var cmd = new ShipOrderCommand(order.Id, "TRK123456");

        // Act
        var result = await handler.Handle(cmd, default);

        // Assert
        Assert.False(result.IsSuccess);
    }
}

public class CancelOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_PendingOrder_CancelsSuccessfully()
    {
        // Arrange
        var order = CreateTestOrder();
        var repo = new FakeOrderRepository();
        repo.Seed(order);
        var uow = new FakeUnitOfWork();

        var handler = new CancelOrderCommandHandler(repo, uow);
        var cmd = new CancelOrderCommand(order.Id, "Customer requested");

        // Act
        var result = await handler.Handle(cmd, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Cancelled", result.Data.Status);
    }

    [Fact]
    public async Task Handle_ShippedOrder_ReturnsFailed()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Confirm();
        order.Ship("TRK123456");

        var repo = new FakeOrderRepository();
        repo.Seed(order);
        var uow = new FakeUnitOfWork();

        var handler = new CancelOrderCommandHandler(repo, uow);
        var cmd = new CancelOrderCommand(order.Id, "Too late");

        // Act
        var result = await handler.Handle(cmd, default);

        // Assert
        Assert.False(result.IsSuccess);
    }
}

public class GetOrderByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingOrder_ReturnsOrderData()
    {
        // Arrange
        var order = CreateTestOrder();
        var repo = new FakeOrderRepository();
        repo.Seed(order);

        var handler = new GetOrderByIdQueryHandler(repo);
        var query = new GetOrderByIdQuery(order.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(order.Id, result.Data.Id);
    }

    [Fact]
    public async Task Handle_UnknownOrder_ReturnsFailed()
    {
        // Arrange
        var repo = new FakeOrderRepository();
        var handler = new GetOrderByIdQueryHandler(repo);
        var query = new GetOrderByIdQuery(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        Assert.False(result.IsSuccess);
    }
}

// Helper
private static Order CreateTestOrder()
{
    var items = new List<OrderItemData>
    {
        new(Guid.NewGuid(), "Test Product", 100m, 1, null)
    };
    var address = ShippingAddress.Create("123 Main St", "NYC", "USA", "10001").Data!;
    var payment = PaymentInfo.Create("PAY123", "card", 115m, DateTime.UtcNow).Data!;

    return Order.Place(Guid.NewGuid(), address, items, 10m, 5m, payment).Data!;
}

// Interfaces needed for mocking
public interface IOrderingDbReader
{
    Task<List<ProductData>> GetProductsAsync(List<Guid> productIds, CancellationToken ct);
    Task<AddressData?> GetShippingAddressAsync(Guid userId, Guid addressId, CancellationToken ct);
    Task<(decimal Discount, Guid PromoId)?> GetPromoCodeAsync(string code, CancellationToken ct);
}

public interface IOrderingCurrentUserService
{
    Guid? UserId { get; }
}

public record ProductData(Guid ProductId, string ProductName, decimal UnitPrice, string? ImageUrl);
public record AddressData(string Street, string City, string Country, string PostalCode);
