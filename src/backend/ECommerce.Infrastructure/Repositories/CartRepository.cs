using System.Threading;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Cart entity providing data access operations.
/// </summary>
public class CartRepository : Repository<Cart>, ICartRepository
{
    public CartRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Retrieves a cart by user ID with all items and products.
    /// </summary>
    public async Task<Cart?> GetByUserIdAsync(Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Include(c => c.Items)
            .ThenInclude(ci => ci.Product)
                .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    /// <summary>
    /// Retrieves a cart by session ID with all items and products.
    /// </summary>
    public async Task<Cart?> GetBySessionIdAsync(string sessionId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Include(c => c.Items)
            .ThenInclude(ci => ci.Product)
                .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId, cancellationToken);
    }

    /// <summary>
    /// Retrieves a specific cart with all its items and products.
    /// </summary>
    public async Task<Cart?> GetCartWithItemsAsync(Guid cartId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Include(c => c.Items)
            .ThenInclude(ci => ci.Product)
                .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(c => c.Id == cartId, cancellationToken);
    }

    /// <summary>
    /// Checks if a cart exists for the specified user.
    /// </summary>
    public async Task<bool> CartExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(c => c.UserId == userId, cancellationToken);
    }

    /// <summary>
    /// Calculates the total value of all items in a cart using SQL aggregation (efficient).
    /// </summary>
    public async Task<decimal> CalculateTotalAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        var total = await DbSet
            .Where(c => c.Id == cartId)
            .Include(c => c.Items)
            .ThenInclude(ci => ci.Product)
            .SelectMany(c => c.Items)
            .SumAsync(ci => ci.Quantity * ci.Product.Price, cancellationToken);

        return total;
    }

    /// <summary>
    /// Gets the total count of items in a cart.
    /// </summary>
    public async Task<int> GetCartItemCountAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Id == cartId)
            .Select(c => c.Items.Count)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
