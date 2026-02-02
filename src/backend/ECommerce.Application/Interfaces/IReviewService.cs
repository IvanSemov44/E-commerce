using ECommerce.Application.DTOs.Products;
using ECommerce.Application.DTOs.Reviews;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for managing product reviews.
/// </summary>
public interface IReviewService
{
    Task<IEnumerable<ReviewDto>> GetProductReviewsAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReviewDetailDto>> GetUserReviewsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ReviewDetailDto> GetReviewByIdAsync(Guid reviewId, CancellationToken cancellationToken = default);
    Task<ReviewDetailDto> CreateReviewAsync(Guid userId, CreateReviewDto dto, CancellationToken cancellationToken = default);
    Task<ReviewDetailDto> UpdateReviewAsync(Guid userId, Guid reviewId, UpdateReviewDto dto, CancellationToken cancellationToken = default);
    Task DeleteReviewAsync(Guid userId, Guid reviewId, CancellationToken cancellationToken = default);
    Task<ReviewDetailDto> ApproveReviewAsync(Guid reviewId, CancellationToken cancellationToken = default);
    Task<ReviewDetailDto> RejectReviewAsync(Guid reviewId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReviewDetailDto>> GetPendingReviewsAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetProductAverageRatingAsync(Guid productId, CancellationToken cancellationToken = default);
}
