using ECommerce.Application.DTOs.Products;
using ECommerce.Application.DTOs.Reviews;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for managing product reviews.
/// </summary>
public interface IReviewService
{
    Task<IEnumerable<ReviewDto>> GetProductReviewsAsync(Guid productId);
    Task<IEnumerable<ReviewDetailDto>> GetUserReviewsAsync(Guid userId);
    Task<ReviewDetailDto> GetReviewByIdAsync(Guid reviewId);
    Task<ReviewDetailDto> CreateReviewAsync(Guid userId, CreateReviewDto dto);
    Task<ReviewDetailDto> UpdateReviewAsync(Guid userId, Guid reviewId, UpdateReviewDto dto);
    Task DeleteReviewAsync(Guid userId, Guid reviewId);
    Task<ReviewDetailDto> ApproveReviewAsync(Guid reviewId);
    Task<ReviewDetailDto> RejectReviewAsync(Guid reviewId);
    Task<IEnumerable<ReviewDetailDto>> GetPendingReviewsAsync();
    Task<decimal> GetProductAverageRatingAsync(Guid productId);
}
