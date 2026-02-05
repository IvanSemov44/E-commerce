using System.Threading;
using ECommerce.Core.Entities;
using ECommerce.Core.Enums;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Order entity providing data access operations.
/// </summary>
public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Retrieves a complete order by order number with all related entities.
    /// </summary>
    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Include(o => o.User)
            .Include(o => o.ShippingAddress)
            .Include(o => o.BillingAddress)
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.PromoCode)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
    }

    /// <summary>
    /// Retrieves paginated orders for a specific user.
    /// </summary>
    public async Task<IEnumerable<Order>> GetUserOrdersAsync(Guid userId, int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Where(o => o.UserId == userId)
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.ShippingAddress)
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the total count of orders for a specific user.
    /// </summary>
    public async Task<int> GetUserOrdersCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(o => o.UserId == userId, cancellationToken);
    }

    /// <summary>
    /// Retrieves an order with all its items and related addresses.
    /// </summary>
    public async Task<Order?> GetWithItemsAsync(Guid orderId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.ShippingAddress)
            .Include(o => o.BillingAddress)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    /// <summary>
    /// Retrieves paginated orders across the entire system.
    /// </summary>
    public async Task<IEnumerable<Order>> GetAllOrdersPaginatedAsync(int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.ShippingAddress)
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the total count of all orders in the system.
    /// </summary>
    public async Task<int> GetTotalOrdersCountAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Calculates the total revenue from paid orders.
    /// </summary>
    public async Task<decimal> GetTotalRevenueAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(o => o.PaymentStatus == PaymentStatus.Paid)
            .SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0;
    }

    /// <summary>
    /// Retrieves order trends for the specified number of days.
    /// </summary>
    public async Task<Dictionary<DateTime, int>> GetOrdersTrendAsync(int days, CancellationToken cancellationToken = default)
    {
        var startDate = DateTime.UtcNow.AddDays(-days).Date;
        var orders = await DbSet
            .Where(o => o.CreatedAt >= startDate)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return orders.ToDictionary(x => x.Date, x => x.Count);
    }

    /// <summary>
    /// Retrieves revenue trends for the specified number of days.
    /// </summary>
    public async Task<Dictionary<DateTime, decimal>> GetRevenueTrendAsync(int days, CancellationToken cancellationToken = default)
    {
        var startDate = DateTime.UtcNow.AddDays(-days).Date;
        var revenue = await DbSet
            .Where(o => o.CreatedAt >= startDate && o.PaymentStatus == PaymentStatus.Paid)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(o => o.TotalAmount) })
            .ToListAsync(cancellationToken);

        return revenue.ToDictionary(x => x.Date, x => x.Amount);
    }
}
