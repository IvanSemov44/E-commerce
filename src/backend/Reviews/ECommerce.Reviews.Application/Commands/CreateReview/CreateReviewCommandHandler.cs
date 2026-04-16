using ECommerce.Reviews.Application.DTOs;
using ECommerce.Reviews.Application.Interfaces;
using ECommerce.Reviews.Domain.Aggregates.Review;
using ECommerce.Reviews.Domain.Errors;
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.Reviews.Domain.ValueObjects;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Reviews.Application.CommandHandlers;

public class CreateReviewCommandHandler(
    IReviewRepository reviewRepository,
    ICatalogService catalogService) : IRequestHandler<CreateReviewCommand, Result<ReviewDetailDto>>
{
    public async Task<Result<ReviewDetailDto>> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        if (!await catalogService.ProductExistsAsync(request.ProductId, cancellationToken))
            return Result<ReviewDetailDto>.Fail(ReviewsErrors.ProductNotFound);

        if (await reviewRepository.ExistsAsync(request.ProductId, request.UserId, cancellationToken))
            return Result<ReviewDetailDto>.Fail(ReviewsErrors.DuplicateReview);

        Result<Rating> ratingResult = Rating.Create(request.Rating);
        if (!ratingResult.IsSuccess)
            return Result<ReviewDetailDto>.Fail(ratingResult.GetErrorOrThrow());

        string comment = request.Comment.Trim();
        Result<ReviewContent> contentResult = ReviewContent.Create(request.Title, comment);
        if (!contentResult.IsSuccess)
            return Result<ReviewDetailDto>.Fail(contentResult.GetErrorOrThrow());

        Review review = Review.Create(
            request.ProductId,
            request.UserId,
            ratingResult.GetDataOrThrow(),
            contentResult.GetDataOrThrow(),
            request.OrderId);

        await reviewRepository.AddAsync(review, cancellationToken);

        return Result<ReviewDetailDto>.Ok(review.ToDetailDto());
    }
}
