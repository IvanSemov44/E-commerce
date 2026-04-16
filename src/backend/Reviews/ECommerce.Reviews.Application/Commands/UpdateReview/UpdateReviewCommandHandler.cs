using ECommerce.Reviews.Application.DTOs;
using ECommerce.Reviews.Domain.Aggregates.Review;
using ECommerce.Reviews.Domain.Errors;
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.Reviews.Domain.ValueObjects;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Reviews.Application.CommandHandlers;

public class UpdateReviewCommandHandler(
    IReviewRepository reviewRepository) : IRequestHandler<UpdateReviewCommand, Result<ReviewDetailDto>>
{
    public async Task<Result<ReviewDetailDto>> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        Review? review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result<ReviewDetailDto>.Fail(ReviewsErrors.ReviewNotFound);

        if (!request.IsAdmin && review.UserId != request.UserId)
            return Result<ReviewDetailDto>.Fail(ReviewsErrors.Unauthorized);

        Rating? newRating = null;
        if (request.Rating.HasValue)
        {
            Result<Rating> ratingResult = Rating.Create(request.Rating.Value);
            if (!ratingResult.IsSuccess)
                return Result<ReviewDetailDto>.Fail(ratingResult.GetErrorOrThrow());

            newRating = ratingResult.GetDataOrThrow();
        }

        string? title = string.IsNullOrWhiteSpace(request.Title) ? review.Content.Title : request.Title;
        string comment = string.IsNullOrWhiteSpace(request.Comment) ? review.Content.Body : request.Comment!;
        Result<ReviewContent> newContentResult = ReviewContent.Create(title, comment);
        if (!newContentResult.IsSuccess)
            return Result<ReviewDetailDto>.Fail(newContentResult.GetErrorOrThrow());

        Result editResult = review.Edit(newRating ?? review.Rating, newContentResult.GetDataOrThrow(), DateTime.UtcNow);
        if (!editResult.IsSuccess)
            return Result<ReviewDetailDto>.Fail(editResult.GetErrorOrThrow());

        await reviewRepository.UpsertAsync(review, cancellationToken);

        return Result<ReviewDetailDto>.Ok(review.ToDetailDto());
    }
}
