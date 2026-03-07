using System.Threading;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Review entity providing data access operations.
/// </summary>
public class ReviewRepository : Repository<Review>, IReviewRepository
{
    public ReviewRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<Review?> GetByIdAsync(Guid id, bool trackChanges = true, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Include(r => r.User)
            .Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <summary>
    /// Retrieves reviews for a specific product, optionally filtering by approval status.
    /// </summary>
    public async Task<IEnumerable<Review>> GetByProductIdAsync(Guid productId, bool onlyApproved = true, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var baseQuery = trackChanges ? DbSet : DbSet.AsNoTracking();
        var query = baseQuery.Where(r => r.ProductId == productId);

        if (onlyApproved)
        {
            query = query.Where(r => r.IsApproved);
        }

        return await query
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves paginated reviews for a specific product.
    /// </summary>
    public async Task<IEnumerable<Review>> GetByProductIdAsync(Guid productId, int skip, int take, bool onlyApproved = true, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var baseQuery = trackChanges ? DbSet : DbSet.AsNoTracking();
        var query = baseQuery.Where(r => r.ProductId == productId);

        if (onlyApproved)
        {
            query = query.Where(r => r.IsApproved);
        }

        return await query
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetByProductIdCountAsync(Guid productId, bool onlyApproved = true, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(r => r.ProductId == productId);

        if (onlyApproved)
        {
            query = query.Where(r => r.IsApproved);
        }

        return await query.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves all reviews submitted by a specific user.
    /// </summary>
    public async Task<IEnumerable<Review>> GetByUserIdAsync(Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Where(r => r.UserId == userId)
            .Include(r => r.Product)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Review>> GetByUserIdAsync(Guid userId, int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Where(r => r.UserId == userId)
            .Include(r => r.Product)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetByUserIdCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(r => r.UserId == userId)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves a review with all its related data by ID.
    /// </summary>
    public async Task<Review?> GetByIdWithDetailsAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Include(r => r.User)
            .Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <summary>
    /// Gets the count of approved reviews for a product.
    /// </summary>
    public async Task<int> GetApprovedReviewCountAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(r => r.ProductId == productId && r.IsApproved)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Calculates the average rating for a product from approved reviews.
    /// </summary>
    public async Task<decimal> GetAverageRatingAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var average = await DbSet
            .Where(r => r.ProductId == productId && r.IsApproved)
            .AverageAsync(r => (decimal?)r.Rating, cancellationToken);
        
        return average ?? 0;
    }

    /// <summary>
    /// Checks if a user has already reviewed a specific product.
    /// </summary>
    public async Task<bool> UserHasReviewedAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(r => r.UserId == userId && r.ProductId == productId, cancellationToken);
    }

    /// <summary>
    /// Retrieves all reviews pending approval.
    /// </summary>
    public async Task<IEnumerable<Review>> GetPendingApprovalAsync(bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Where(r => !r.IsApproved)
            .Include(r => r.User)
            .Include(r => r.Product)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Review>> GetPendingApprovalAsync(int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Where(r => !r.IsApproved)
            .Include(r => r.User)
            .Include(r => r.Product)
            .OrderBy(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetPendingApprovalCountAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(r => !r.IsApproved)
            .CountAsync(cancellationToken);
    }
}
