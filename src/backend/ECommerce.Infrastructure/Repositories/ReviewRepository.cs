using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class ReviewRepository : Repository<Review>, IReviewRepository
{
    public ReviewRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Review>> GetByProductIdAsync(Guid productId, bool onlyApproved = true, bool trackChanges = false)
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
            .ToListAsync();
    }

    public async Task<IEnumerable<Review>> GetByUserIdAsync(Guid userId, bool trackChanges = false)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Where(r => r.UserId == userId)
            .Include(r => r.Product)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Review?> GetByIdWithDetailsAsync(Guid id, bool trackChanges = false)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Include(r => r.User)
            .Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<int> GetApprovedReviewCountAsync(Guid productId)
    {
        return await DbSet
            .Where(r => r.ProductId == productId && r.IsApproved)
            .CountAsync();
    }

    public async Task<decimal> GetAverageRatingAsync(Guid productId)
    {
        var reviews = await DbSet
            .Where(r => r.ProductId == productId && r.IsApproved)
            .Select(r => r.Rating)
            .ToListAsync();

        if (reviews.Count == 0) return 0;
        return (decimal)reviews.Average();
    }

    public async Task<bool> UserHasReviewedAsync(Guid userId, Guid productId)
    {
        return await DbSet.AnyAsync(r => r.UserId == userId && r.ProductId == productId);
    }

    public async Task<IEnumerable<Review>> GetPendingApprovalAsync(bool trackChanges = false)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Where(r => !r.IsApproved)
            .Include(r => r.User)
            .Include(r => r.Product)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }
}
