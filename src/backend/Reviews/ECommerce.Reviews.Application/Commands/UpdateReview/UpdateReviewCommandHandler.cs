namespace ECommerce.Reviews.Application.Commands.UpdateReview;

public class UpdateReviewCommandHandler(
    IReviewRepository reviewRepository) : IRequestHandler<UpdateReviewCommand, Result>
{
    public async Task<Result> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result.Fail(ReviewsErrors.ReviewNotFound);

        if (!request.IsAdmin && review.UserId != request.UserId)
            return Result.Fail(ReviewsErrors.Unauthorized);

        return review.Edit(request.Rating, request.Title, request.Comment);
    }
}
