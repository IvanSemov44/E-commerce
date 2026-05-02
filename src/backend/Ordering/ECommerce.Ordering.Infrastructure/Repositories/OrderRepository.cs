using ECommerce.Ordering.Domain.Aggregates.Order;
using ECommerce.Ordering.Domain.Interfaces;
using ECommerce.Ordering.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Infrastructure.Repositories;

public class OrderRepository(OrderingDbContext _db) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default)
        => _db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<List<Order>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _db.Orders.AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public Task<List<Order>> GetAllAsync(CancellationToken ct = default)
        => _db.Orders.AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public Task<int> GetTotalOrdersCountAsync(CancellationToken ct = default)
        => _db.Orders.AsNoTracking().CountAsync(ct);

    public async Task<decimal> GetTotalRevenueAsync(CancellationToken ct = default)
        => await _db.Orders.AsNoTracking().SumAsync(o => (decimal?)o.Total, ct) ?? 0m;

    public async Task<Dictionary<DateTime, int>> GetOrdersTrendAsync(int days, CancellationToken ct = default)
    {
        var startDate = DateTime.UtcNow.AddDays(-days).Date;
        var data = await _db.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= startDate)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        return data.ToDictionary(x => x.Date, x => x.Count);
    }

    public async Task<Dictionary<DateTime, decimal>> GetRevenueTrendAsync(int days, CancellationToken ct = default)
    {
        var startDate = DateTime.UtcNow.AddDays(-days).Date;
        var data = await _db.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= startDate)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(o => o.Total) })
            .ToListAsync(ct);
        return data.ToDictionary(x => x.Date, x => x.Amount);
    }

    public Task AddAsync(Order order, CancellationToken ct = default)
    {
        _db.Orders.Add(order);
        return Task.CompletedTask;
    }
}
