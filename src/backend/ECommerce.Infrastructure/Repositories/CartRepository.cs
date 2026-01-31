using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class CartRepository : Repository<Cart>, ICartRepository
{
    public CartRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Cart?> GetByUserIdAsync(Guid userId, bool trackChanges = false)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Include(c => c.Items)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<Cart?> GetBySessionIdAsync(string sessionId, bool trackChanges = false)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Include(c => c.Items)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId);
    }

    public async Task<Cart?> GetCartWithItemsAsync(Guid cartId, bool trackChanges = false)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Include(c => c.Items)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.Id == cartId);
    }

    public async Task<bool> CartExistsForUserAsync(Guid userId)
    {
        return await DbSet.AnyAsync(c => c.UserId == userId);
    }

    public async Task<decimal> CalculateTotalAsync(Guid cartId)
    {
        var cart = await DbSet
            .Include(c => c.Items)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart == null) return 0;

        return cart.Items.Sum(item => item.Quantity * item.Product.Price);
    }

    public async Task<int> GetCartItemCountAsync(Guid cartId)
    {
        return await DbSet
            .Where(c => c.Id == cartId)
            .SelectMany(c => c.Items)
            .CountAsync();
    }
}
