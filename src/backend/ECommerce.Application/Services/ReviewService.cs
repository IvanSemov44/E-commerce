using ECommerce.Application.Interfaces;
using AutoMapper;
using ECommerce.Application.DTOs.Reviews;
using ECommerce.Application.DTOs.Products;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Results;
using ECommerce.Core.Constants;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace ECommerce.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ReviewService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<ReviewDto>>> GetProductReviewsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId, trackChanges: false, cancellationToken: cancellationToken);
        if (product == null)
            return Result<IEnumerable<ReviewDto>>.Fail(ErrorCodes.ProductNotFound, $"Product with id '{productId}' not found");

        var reviews = await _unitOfWork.Reviews.GetByProductIdAsync(productId, onlyApproved: true, cancellationToken: cancellationToken);
        return Result<IEnumerable<ReviewDto>>.Ok(_mapper.Map<IEnumerable<ReviewDto>>(reviews));
    }

    public async Task<Result<IEnumerable<ReviewDetailDto>>> GetUserReviewsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
            return Result<IEnumerable<ReviewDetailDto>>.Fail(ErrorCodes.UserNotFound, $"User with id '{userId}' not found");

        var reviews = await _unitOfWork.Reviews.GetByUserIdAsync(userId, cancellationToken: cancellationToken);
        return Result<IEnumerable<ReviewDetailDto>>.Ok(_mapper.Map<IEnumerable<ReviewDetailDto>>(reviews));
    }

    public async Task<Result<ReviewDetailDto>> GetReviewByIdAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _unitOfWork.Reviews.GetByIdWithDetailsAsync(reviewId, cancellationToken: cancellationToken);
        if (review == null)
            return Result<ReviewDetailDto>.Fail(ErrorCodes.ReviewNotFound, $"Review with id '{reviewId}' not found");

        return Result<ReviewDetailDto>.Ok(_mapper.Map<ReviewDetailDto>(review));
    }

    public async Task<Result<ReviewDetailDto>> CreateReviewAsync(Guid userId, CreateReviewDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
            return Result<ReviewDetailDto>.Fail(ErrorCodes.InvalidRating, "Rating must be between 1 and 5");

        if (string.IsNullOrWhiteSpace(dto.Comment))
            return Result<ReviewDetailDto>.Fail(ErrorCodes.EmptyReviewComment, "Review comment cannot be empty");

        var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId, trackChanges: false, cancellationToken: cancellationToken);
        if (product == null)
            return Result<ReviewDetailDto>.Fail(ErrorCodes.ProductNotFound, $"Product with id '{dto.ProductId}' not found");

        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
            return Result<ReviewDetailDto>.Fail(ErrorCodes.UserNotFound, $"User with id '{userId}' not found");

        if (await _unitOfWork.Reviews.UserHasReviewedAsync(userId, dto.ProductId, cancellationToken: cancellationToken))
            return Result<ReviewDetailDto>.Fail(ErrorCodes.DuplicateReview, "You have already reviewed this product");

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

        await _unitOfWork.Reviews.AddAsync(review, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result<ReviewDetailDto>.Ok(_mapper.Map<ReviewDetailDto>(review));
    }

    public async Task<Result<ReviewDetailDto>> UpdateReviewAsync(Guid userId, Guid reviewId, UpdateReviewDto dto, CancellationToken cancellationToken = default)
    {
        var review = await _unitOfWork.Reviews.GetByIdWithDetailsAsync(reviewId, cancellationToken: cancellationToken);
        if (review == null)
            return Result<ReviewDetailDto>.Fail(ErrorCodes.ReviewNotFound, $"Review with id '{reviewId}' not found");

        if (review.UserId != userId)
            return Result<ReviewDetailDto>.Fail(ErrorCodes.Unauthorized, "You can only update your own reviews");

        if (DateTime.UtcNow - review.CreatedAt > TimeSpan.FromHours(24))
            return Result<ReviewDetailDto>.Fail(ErrorCodes.ReviewUpdateExpired, "You can only update reviews within 24 hours of creation");

        if (!string.IsNullOrWhiteSpace(dto.Title))
            review.Title = dto.Title;

        if (!string.IsNullOrWhiteSpace(dto.Comment))
            review.Comment = dto.Comment;

        if (dto.Rating.HasValue)
        {
            if (dto.Rating < 1 || dto.Rating > 5)
                return Result<ReviewDetailDto>.Fail(ErrorCodes.InvalidRating, "Rating must be between 1 and 5");
            review.Rating = dto.Rating.Value;
        }

        review.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Reviews.UpdateAsync(review, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result<ReviewDetailDto>.Ok(_mapper.Map<ReviewDetailDto>(review));
    }

    public async Task<Result<Unit>> DeleteReviewAsync(Guid userId, Guid reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId, trackChanges: false, cancellationToken: cancellationToken);
        if (review == null)
            return Result<Unit>.Fail(ErrorCodes.ReviewNotFound, $"Review with id '{reviewId}' not found");

        if (review.UserId != userId)
            return Result<Unit>.Fail(ErrorCodes.Unauthorized, "You can only delete your own reviews");

        await _unitOfWork.Reviews.DeleteAsync(review, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        
        return Result<Unit>.Ok(new Unit());
    }

    public async Task<Result<ReviewDetailDto>> ApproveReviewAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _unitOfWork.Reviews.GetByIdWithDetailsAsync(reviewId, cancellationToken: cancellationToken);
        if (review == null)
            return Result<ReviewDetailDto>.Fail(ErrorCodes.ReviewNotFound, $"Review with id '{reviewId}' not found");

        review.IsApproved = true;
        review.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Reviews.UpdateAsync(review, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result<ReviewDetailDto>.Ok(_mapper.Map<ReviewDetailDto>(review));
    }

    public async Task<Result<ReviewDetailDto>> RejectReviewAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _unitOfWork.Reviews.GetByIdWithDetailsAsync(reviewId, cancellationToken: cancellationToken);
        if (review == null)
            return Result<ReviewDetailDto>.Fail(ErrorCodes.ReviewNotFound, $"Review with id '{reviewId}' not found");

        var mappedDto = _mapper.Map<ReviewDetailDto>(review);
        await _unitOfWork.Reviews.DeleteAsync(review, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result<ReviewDetailDto>.Ok(mappedDto);
    }

    public async Task<IEnumerable<ReviewDetailDto>> GetPendingReviewsAsync(CancellationToken cancellationToken = default)
    {
        var reviews = await _unitOfWork.Reviews.GetPendingApprovalAsync(cancellationToken: cancellationToken);
        return _mapper.Map<IEnumerable<ReviewDetailDto>>(reviews);
    }

    public async Task<decimal> GetProductAverageRatingAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Reviews.GetAverageRatingAsync(productId, cancellationToken: cancellationToken);
    }
}
