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
    public Task<Wishlist?> GetByUserIdAsync(Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return query
            .Where(w => w.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves a wishlist by user ID with all wishlist items and products.
    /// </summary>
    public Task<Wishlist?> GetByUserIdWithItemsAsync(Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return query
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
    public Task<bool> IsProductInWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(w => w.UserId == userId && w.ProductId == productId, cancellationToken);

    /// <summary>
    /// Gets the total count of items in a user's wishlist.
    /// </summary>
    public Task<int> GetWishlistItemCountAsync(Guid userId, CancellationToken cancellationToken = default)
        => DbSet
            .Where(w => w.UserId == userId)
            .CountAsync(cancellationToken);

    /// <summary>
    /// Gets all wishlist entries for a user with product details.
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

    /// <summary>
    /// Deletes all wishlist entries for a user in a single bulk DB operation.
    /// Uses ExecuteDeleteAsync to avoid loading any entities into memory.
    /// Note: ExecuteDeleteAsync bypasses the change tracker and commits immediately.
    /// </summary>
    public async Task ClearByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await DbSet
            .Where(w => w.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
