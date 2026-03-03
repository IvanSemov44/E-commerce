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
                .ThenInclude(p => p.Images)
            .Include(w => w.Product)
                .ThenInclude(p => p.Category)
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

    /// <summary>
    /// Gets all wishlist entries for a user with product details.
    /// FIX: Uses database-level filtering instead of loading ALL entries into memory.
    /// </summary>
    public async Task<IEnumerable<Wishlist>> GetAllByUserIdAsync(Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Where(w => w.UserId == userId)
            .Include(w => w.Product)
                .ThenInclude(p => p.Images)
            .Include(w => w.Product)
                .ThenInclude(p => p.Category)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes a wishlist entry by user ID and product ID.
    /// FIX: Efficient deletion without loading all entries.
    /// </summary>
    public async Task DeleteByUserIdAndProductIdAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, cancellationToken);
        
        if (entry != null)
        {
            DbSet.Remove(entry);
        }
    }
}
