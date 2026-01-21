using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class WishlistRepository : Repository<Wishlist>, IWishlistRepository
{
    public WishlistRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Wishlist?> GetByUserIdAsync(Guid userId)
    {
        return await DbSet
            .Where(w => w.UserId == userId)
            .FirstOrDefaultAsync();
    }

    public async Task<Wishlist?> GetByUserIdWithItemsAsync(Guid userId)
    {
        return await DbSet
            .Where(w => w.UserId == userId)
            .Include(w => w.Product)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsProductInWishlistAsync(Guid userId, Guid productId)
    {
        return await DbSet.AnyAsync(w => w.UserId == userId && w.ProductId == productId);
    }

    public async Task<int> GetWishlistItemCountAsync(Guid userId)
    {
        return await DbSet
            .Where(w => w.UserId == userId)
            .CountAsync();
    }

    public async Task<Wishlist?> GetOrCreateForUserAsync(Guid userId)
    {
        // For junction table model, we don't create an empty wishlist
        // We only create entries when products are added
        return null;
    }
}
