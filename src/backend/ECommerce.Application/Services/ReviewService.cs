using ECommerce.Application.Interfaces;
using AutoMapper;
using ECommerce.Application.DTOs.Reviews;
using ECommerce.Application.DTOs.Products;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Core.Exceptions;

namespace ECommerce.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ReviewService(
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ReviewDto>> GetProductReviewsAsync(Guid productId)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId, trackChanges: false);
        if (product == null)
            throw new ProductNotFoundException(productId);

        var reviews = await _unitOfWork.Reviews.GetByProductIdAsync(productId, onlyApproved: true);
        return _mapper.Map<IEnumerable<ReviewDto>>(reviews);
    }

    public async Task<IEnumerable<ReviewDetailDto>> GetUserReviewsAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false);
        if (user == null)
            throw new UserNotFoundException(userId);

        var reviews = await _unitOfWork.Reviews.GetByUserIdAsync(userId);
        return _mapper.Map<IEnumerable<ReviewDetailDto>>(reviews);
    }

    public async Task<ReviewDetailDto> GetReviewByIdAsync(Guid reviewId)
    {
        var review = await _unitOfWork.Reviews.GetByIdWithDetailsAsync(reviewId);
        if (review == null)
            throw new ReviewNotFoundException(reviewId);

        return _mapper.Map<ReviewDetailDto>(review);
    }

    public async Task<ReviewDetailDto> CreateReviewAsync(Guid userId, CreateReviewDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
            throw new InvalidRatingException();

        if (string.IsNullOrWhiteSpace(dto.Comment))
            throw new EmptyReviewCommentException();

        var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId, trackChanges: false);
        if (product == null)
            throw new ProductNotFoundException(dto.ProductId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false);
        if (user == null)
            throw new UserNotFoundException(userId);

        if (await _unitOfWork.Reviews.UserHasReviewedAsync(userId, dto.ProductId))
            throw new DuplicateReviewException();

        var review = new Review
        {
            ProductId = dto.ProductId,
            UserId = userId,
            Title = dto.Title,
            Comment = dto.Comment,
            Rating = dto.Rating,
            IsVerified = false,
            IsApproved = false
        };

        await _unitOfWork.Reviews.AddAsync(review);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ReviewDetailDto>(review);
    }

    public async Task<ReviewDetailDto> UpdateReviewAsync(Guid userId, Guid reviewId, UpdateReviewDto dto)
    {
        var review = await _unitOfWork.Reviews.GetByIdWithDetailsAsync(reviewId);
        if (review == null)
            throw new ReviewNotFoundException(reviewId);

        if (review.UserId != userId)
            throw new UnauthorizedAccessException("You can only update your own reviews");

        if (DateTime.UtcNow - review.CreatedAt > TimeSpan.FromHours(24))
            throw new ReviewUpdateTimeExpiredException();

        if (!string.IsNullOrWhiteSpace(dto.Title))
            review.Title = dto.Title;

        if (!string.IsNullOrWhiteSpace(dto.Comment))
            review.Comment = dto.Comment;

        if (dto.Rating.HasValue)
        {
            if (dto.Rating < 1 || dto.Rating > 5)
                throw new InvalidRatingException();
            review.Rating = dto.Rating.Value;
        }

        review.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Reviews.UpdateAsync(review);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ReviewDetailDto>(review);
    }

    public async Task DeleteReviewAsync(Guid userId, Guid reviewId)
    {
        var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId, trackChanges: false);
        if (review == null)
            throw new ReviewNotFoundException(reviewId);

        if (review.UserId != userId)
            throw new UnauthorizedAccessException("You can only delete your own reviews");

        await _unitOfWork.Reviews.DeleteAsync(review);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<ReviewDetailDto> ApproveReviewAsync(Guid reviewId)
    {
        var review = await _unitOfWork.Reviews.GetByIdWithDetailsAsync(reviewId);
        if (review == null)
            throw new ReviewNotFoundException(reviewId);

        review.IsApproved = true;
        review.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Reviews.UpdateAsync(review);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ReviewDetailDto>(review);
    }

    public async Task<ReviewDetailDto> RejectReviewAsync(Guid reviewId)
    {
        var review = await _unitOfWork.Reviews.GetByIdWithDetailsAsync(reviewId);
        if (review == null)
            throw new ReviewNotFoundException(reviewId);

        await _unitOfWork.Reviews.DeleteAsync(review);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ReviewDetailDto>(review);
    }

    public async Task<IEnumerable<ReviewDetailDto>> GetPendingReviewsAsync()
    {
        var reviews = await _unitOfWork.Reviews.GetPendingApprovalAsync();
        return _mapper.Map<IEnumerable<ReviewDetailDto>>(reviews);
    }

    public async Task<decimal> GetProductAverageRatingAsync(Guid productId)
    {
        return await _unitOfWork.Reviews.GetAverageRatingAsync(productId);
    }
}
