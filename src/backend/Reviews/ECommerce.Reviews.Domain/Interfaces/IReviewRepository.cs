using ECommerce.Reviews.Domain.Aggregates.Review;

namespace ECommerce.Reviews.Domain.Interfaces;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Review?> GetByProductAndAuthorAsync(Guid productId, Guid authorId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Review> Items, int TotalCount)> GetByProductAsync(
        Guid productId,
        int page,
        int pageSize,
        bool onlyApproved = true,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Review> Items, int TotalCount)> GetByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Review> Items, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        string? status,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Review> Items, int TotalCount)> GetPendingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Review> Items, int TotalCount)> GetFlaggedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<decimal> GetAverageRatingAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid productId, Guid authorId, CancellationToken cancellationToken = default);

    Task AddAsync(Review review, CancellationToken cancellationToken = default);
}
