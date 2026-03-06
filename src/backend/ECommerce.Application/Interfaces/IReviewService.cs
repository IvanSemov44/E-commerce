using ECommerce.Application.DTOs.Products;
using ECommerce.Application.DTOs.Reviews;
using ECommerce.Core.Results;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for managing product reviews.
/// </summary>
public interface IReviewService
{
    Task<Result<IEnumerable<ReviewDto>>> GetProductReviewsAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ReviewDetailDto>>> GetUserReviewsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<ReviewDetailDto>> GetReviewByIdAsync(Guid reviewId, CancellationToken cancellationToken = default);
    Task<Result<ReviewDetailDto>> CreateReviewAsync(Guid userId, CreateReviewDto dto, CancellationToken cancellationToken = default);
    Task<Result<ReviewDetailDto>> UpdateReviewAsync(Guid userId, Guid reviewId, UpdateReviewDto dto, CancellationToken cancellationToken = default);
    Task<Result<ReviewDetailDto>> UpdateReviewAsync(Guid userId, Guid reviewId, UpdateReviewDto dto, bool isAdmin, CancellationToken cancellationToken = default);
    Task<Result<Unit>> DeleteReviewAsync(Guid userId, Guid reviewId, CancellationToken cancellationToken = default);
    Task<Result<Unit>> DeleteReviewAsync(Guid userId, Guid reviewId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<Result<ReviewDetailDto>> ApproveReviewAsync(Guid reviewId, CancellationToken cancellationToken = default);
    Task<Result<ReviewDetailDto>> RejectReviewAsync(Guid reviewId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReviewDetailDto>> GetPendingReviewsAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetProductAverageRatingAsync(Guid productId, CancellationToken cancellationToken = default);
}
