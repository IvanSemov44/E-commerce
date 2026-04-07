using ECommerce.Ordering.Application.Queries.GetOrderStats;
using ECommerce.Ordering.Domain.Aggregates.Order;
using ECommerce.Ordering.Domain.Interfaces;
using ECommerce.Ordering.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Ordering.Tests.Handlers;

[TestClass]
public class GetOrderStatsQueryHandlerTests
{
    private sealed class FakeOrderRepository : IOrderRepository
    {
        private readonly List<Order> _store = new();

        public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_store.FirstOrDefault(x => x.Id == id));

        public Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_store.FirstOrDefault(x => x.Id == id));

        public Task<List<Order>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult(_store.Where(x => x.UserId == userId).ToList());

        public Task<List<Order>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(_store.ToList());

        public Task<int> GetTotalOrdersCountAsync(CancellationToken ct = default)
            => Task.FromResult(_store.Count);

        public Task<decimal> GetTotalRevenueAsync(CancellationToken ct = default)
            => Task.FromResult(_store.Sum(x => x.Total));

        public Task<Dictionary<DateTime, int>> GetOrdersTrendAsync(int days, CancellationToken ct = default)
        {
            var start = DateTime.UtcNow.AddDays(-days).Date;
            var data = _store
                .Where(o => o.CreatedAt.Date >= start)
                .GroupBy(o => o.CreatedAt.Date)
                .ToDictionary(g => g.Key, g => g.Count());
            return Task.FromResult(data);
        }

        public Task<Dictionary<DateTime, decimal>> GetRevenueTrendAsync(int days, CancellationToken ct = default)
        {
            var start = DateTime.UtcNow.AddDays(-days).Date;
            var data = _store
                .Where(o => o.CreatedAt.Date >= start)
                .GroupBy(o => o.CreatedAt.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Total));
            return Task.FromResult(data);
        }

        public Task AddAsync(Order order, CancellationToken ct = default)
        {
            _store.Add(order);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Order order, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    [TestMethod]
    public async Task Handle_ReturnsAggregatedStatsAndTrends()
    {
        var repo = new FakeOrderRepository();
        await repo.AddAsync(CreateOrder(100m));
        await repo.AddAsync(CreateOrder(250m));

        var handler = new GetOrderStatsQueryHandler(repo);
        var result = await handler.Handle(new GetOrderStatsQuery(30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var data = result.GetDataOrThrow();
        data.TotalOrders.Should().Be(2);
        data.TotalRevenue.Should().Be(350m);
        data.OrdersTrend.Should().NotBeEmpty();
        data.RevenueTrend.Should().NotBeEmpty();
    }

    private static Order CreateOrder(decimal amount)
    {
        var items = new List<OrderItemData>
        {
            new(Guid.NewGuid(), "Product", amount, 1, null)
        };
        var shipping = ShippingAddress.Create("Street", "City", "US", "00000");
        var payment = PaymentInfo.Create(Guid.NewGuid().ToString("N"), "card", amount, DateTime.UtcNow).GetDataOrThrow();
        return Order.Place(Guid.NewGuid(), shipping, items, 0m, 0m, payment).GetDataOrThrow();
    }
}
