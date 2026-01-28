using ECommerce.Core.Entities;
using ECommerce.Core.Enums;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
    {
        return await DbSet
            .Include(o => o.User)
            .Include(o => o.ShippingAddress)
            .Include(o => o.BillingAddress)
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.PromoCode)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<IEnumerable<Order>> GetUserOrdersAsync(Guid userId, int skip, int take)
    {
        return await DbSet
            .Where(o => o.UserId == userId)
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.ShippingAddress)
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> GetUserOrdersCountAsync(Guid userId)
    {
        return await DbSet.CountAsync(o => o.UserId == userId);
    }

    public async Task<Order?> GetWithItemsAsync(Guid orderId)
    {
        return await DbSet
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.ShippingAddress)
            .Include(o => o.BillingAddress)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<int> GetTotalOrdersCountAsync()
    {
        return await DbSet.CountAsync();
    }

    public async Task<decimal> GetTotalRevenueAsync()
    {
        return await DbSet
            .Where(o => o.PaymentStatus == PaymentStatus.Paid)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
    }

    public async Task<Dictionary<DateTime, int>> GetOrdersTrendAsync(int days)
    {
        var startDate = DateTime.UtcNow.AddDays(-days).Date;
        var orders = await DbSet
            .Where(o => o.CreatedAt >= startDate)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        return orders.ToDictionary(x => x.Date, x => x.Count);
    }

    public async Task<Dictionary<DateTime, decimal>> GetRevenueTrendAsync(int days)
    {
        var startDate = DateTime.UtcNow.AddDays(-days).Date;
        var revenue = await DbSet
            .Where(o => o.CreatedAt >= startDate && o.PaymentStatus == PaymentStatus.Paid)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(o => o.TotalAmount) })
            .ToListAsync();

        return revenue.ToDictionary(x => x.Date, x => x.Amount);
    }
}
