using AutoMapper;
using ECommerce.Application.DTOs.Reviews;
using ECommerce.Application.DTOs.Products;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;

namespace ECommerce.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public ReviewService(
        IReviewRepository reviewRepository,
        IProductRepository productRepository,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _reviewRepository = reviewRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ReviewDto>> GetProductReviewsAsync(Guid productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
            throw new InvalidOperationException($"Product {productId} not found");

        var reviews = await _reviewRepository.GetByProductIdAsync(productId, onlyApproved: true);
        return _mapper.Map<IEnumerable<ReviewDto>>(reviews);
    }

    public async Task<IEnumerable<ReviewDetailDto>> GetUserReviewsAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException($"User {userId} not found");

        var reviews = await _reviewRepository.GetByUserIdAsync(userId);
        return _mapper.Map<IEnumerable<ReviewDetailDto>>(reviews);
    }

    public async Task<ReviewDetailDto> GetReviewByIdAsync(Guid reviewId)
    {
        var review = await _reviewRepository.GetByIdWithDetailsAsync(reviewId)
            ?? throw new InvalidOperationException($"Review {reviewId} not found");

        return _mapper.Map<ReviewDetailDto>(review);
    }

    public async Task<ReviewDetailDto> CreateReviewAsync(Guid userId, CreateReviewDto dto)
    {
        // Validate rating
        if (dto.Rating < 1 || dto.Rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5");

        if (string.IsNullOrWhiteSpace(dto.Comment))
            throw new ArgumentException("Comment cannot be empty");

        // Verify product exists
        var product = await _productRepository.GetByIdAsync(dto.ProductId)
            ?? throw new InvalidOperationException($"Product {dto.ProductId} not found");

        // Verify user exists
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} not found");

        // Check if user already reviewed this product
        if (await _reviewRepository.UserHasReviewedAsync(userId, dto.ProductId))
            throw new InvalidOperationException("You have already reviewed this product");

        var review = new Review
        {
            ProductId = dto.ProductId,
            UserId = userId,
            Title = dto.Title,
            Comment = dto.Comment,
            Rating = dto.Rating,
            IsVerified = false, // Set to false initially, can be set to true after purchase verification
            IsApproved = false  // Require approval before display
        };

        await _reviewRepository.AddAsync(review);
        await _reviewRepository.SaveChangesAsync();

        return _mapper.Map<ReviewDetailDto>(review);
    }

    public async Task<ReviewDetailDto> UpdateReviewAsync(Guid userId, Guid reviewId, UpdateReviewDto dto)
    {
        var review = await _reviewRepository.GetByIdWithDetailsAsync(reviewId)
            ?? throw new InvalidOperationException($"Review {reviewId} not found");

        // Ensure user owns the review
        if (review.UserId != userId)
            throw new UnauthorizedAccessException("You can only update your own reviews");

        // Only allow updates within 24 hours of creation
        if (DateTime.UtcNow - review.CreatedAt > TimeSpan.FromHours(24))
            throw new InvalidOperationException("Reviews can only be updated within 24 hours of creation");

        if (!string.IsNullOrWhiteSpace(dto.Title))
            review.Title = dto.Title;

        if (!string.IsNullOrWhiteSpace(dto.Comment))
            review.Comment = dto.Comment;

        if (dto.Rating.HasValue)
        {
            if (dto.Rating < 1 || dto.Rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5");
            review.Rating = dto.Rating.Value;
        }

        review.UpdatedAt = DateTime.UtcNow;

        await _reviewRepository.UpdateAsync(review);
        await _reviewRepository.SaveChangesAsync();

        return _mapper.Map<ReviewDetailDto>(review);
    }

    public async Task<bool> DeleteReviewAsync(Guid userId, Guid reviewId)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review == null)
            return false;

        // Ensure user owns the review
        if (review.UserId != userId)
            throw new UnauthorizedAccessException("You can only delete your own reviews");

        await _reviewRepository.DeleteAsync(review);
        await _reviewRepository.SaveChangesAsync();

        return true;
    }

    public async Task<ReviewDetailDto> ApproveReviewAsync(Guid reviewId)
    {
        var review = await _reviewRepository.GetByIdWithDetailsAsync(reviewId)
            ?? throw new InvalidOperationException($"Review {reviewId} not found");

        review.IsApproved = true;
        review.UpdatedAt = DateTime.UtcNow;

        await _reviewRepository.UpdateAsync(review);
        await _reviewRepository.SaveChangesAsync();

        return _mapper.Map<ReviewDetailDto>(review);
    }

    public async Task<ReviewDetailDto> RejectReviewAsync(Guid reviewId)
    {
        var review = await _reviewRepository.GetByIdWithDetailsAsync(reviewId)
            ?? throw new InvalidOperationException($"Review {reviewId} not found");

        await _reviewRepository.DeleteAsync(review);
        await _reviewRepository.SaveChangesAsync();

        return _mapper.Map<ReviewDetailDto>(review);
    }

    public async Task<IEnumerable<ReviewDetailDto>> GetPendingReviewsAsync()
    {
        var reviews = await _reviewRepository.GetPendingApprovalAsync();
        return _mapper.Map<IEnumerable<ReviewDetailDto>>(reviews);
    }

    public async Task<decimal> GetProductAverageRatingAsync(Guid productId)
    {
        return await _reviewRepository.GetAverageRatingAsync(productId);
    }
}
