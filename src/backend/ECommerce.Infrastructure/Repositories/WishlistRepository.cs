using System.Threading;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Wishlist entity providing data access operations.
/// </summary>
public class WishlistRepository : Repository<Wishlist>, IWishlistRepository
{
    public WishlistRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Retrieves a wishlist by user ID without related items.
    /// </summary>
    public async Task<Wishlist?> GetByUserIdAsync(Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Where(w => w.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves a wishlist by user ID with all wishlist items and products.
    /// </summary>
    public async Task<Wishlist?> GetByUserIdWithItemsAsync(Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Where(w => w.UserId == userId)
            .Include(w => w.Product)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if a product is in a user's wishlist.
    /// </summary>
    public async Task<bool> IsProductInWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(w => w.UserId == userId && w.ProductId == productId, cancellationToken);
    }

    /// <summary>
    /// Gets the total count of items in a user's wishlist.
    /// </summary>
    public async Task<int> GetWishlistItemCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(w => w.UserId == userId)
            .CountAsync(cancellationToken);
    }
}
