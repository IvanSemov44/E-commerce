using ECommerce.Core.Entities;
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
}
