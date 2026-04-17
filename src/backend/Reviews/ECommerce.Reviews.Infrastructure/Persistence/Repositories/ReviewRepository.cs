using ECommerce.Reviews.Domain.Aggregates.Review;
using ECommerce.Reviews.Domain.Enums;
using ECommerce.Reviews.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Reviews.Infrastructure.Persistence.Repositories;

public class ReviewRepository(ReviewsDbContext db) : IReviewRepository
{
    public Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => db.Reviews.FirstOrDefaultAsync(review => review.Id == id, cancellationToken);

    public Task<Review?> GetByProductAndAuthorAsync(Guid productId, Guid authorId, CancellationToken cancellationToken = default)
        => db.Reviews.FirstOrDefaultAsync(review => review.ProductId == productId && review.UserId == authorId, cancellationToken);

    public async Task<(IReadOnlyList<Review> Items, int TotalCount)> GetByProductAsync(
        Guid productId,
        int page,
        int pageSize,
        bool onlyApproved = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Review> query = db.Reviews.AsNoTracking()
            .Where(review => review.ProductId == productId);

        if (onlyApproved)
            query = query.Where(review => review.Status == ReviewStatus.Approved);

        query = query.OrderByDescending(review => review.CreatedAt);

        int totalCount = await query.CountAsync(cancellationToken);
        List<Review> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Review> Items, int TotalCount)> GetByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Review> query = db.Reviews.AsNoTracking()
            .Where(review => review.UserId == userId)
            .OrderByDescending(review => review.CreatedAt);

        int totalCount = await query.CountAsync(cancellationToken);
        List<Review> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Review> Items, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        string? status,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Review> query = db.Reviews.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string pattern = $"%{search}%";
            query = query.Where(review =>
                EF.Functions.Like(review.Content.Title ?? string.Empty, pattern) ||
                EF.Functions.Like(review.Content.Body, pattern));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReviewStatus>(status, true, out ReviewStatus parsedStatus))
            query = query.Where(review => review.Status == parsedStatus);

        query = query.OrderByDescending(review => review.CreatedAt);

        int totalCount = await query.CountAsync(cancellationToken);
        List<Review> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Review> Items, int TotalCount)> GetPendingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Review> query = db.Reviews.AsNoTracking()
            .Where(review => review.Status == ReviewStatus.Pending)
            .OrderByDescending(review => review.CreatedAt);

        int totalCount = await query.CountAsync(cancellationToken);
        List<Review> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Review> Items, int TotalCount)> GetFlaggedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Review> query = db.Reviews.AsNoTracking()
            .Where(review => review.Status == ReviewStatus.Flagged)
            .OrderByDescending(review => review.FlagCount)
            .ThenByDescending(review => review.UpdatedAt);

        int totalCount = await query.CountAsync(cancellationToken);
        List<Review> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<decimal> GetAverageRatingAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return GetAverageRatingInternalAsync(productId, cancellationToken);
    }

    private async Task<decimal> GetAverageRatingInternalAsync(Guid productId, CancellationToken cancellationToken)
    {
        decimal? average = await db.Reviews.AsNoTracking()
            .Where(review => review.ProductId == productId && review.Status == ReviewStatus.Approved)
            .Select(review => (decimal?)EF.Property<int>(review, "Rating"))
            .AverageAsync(cancellationToken);

        return average ?? 0m;
    }

    public Task<bool> ExistsAsync(Guid productId, Guid authorId, CancellationToken cancellationToken = default)
        => db.Reviews.AnyAsync(review => review.ProductId == productId && review.UserId == authorId, cancellationToken);

    public Task AddAsync(Review review, CancellationToken cancellationToken = default)
        => db.Reviews.AddAsync(review, cancellationToken).AsTask();
}
