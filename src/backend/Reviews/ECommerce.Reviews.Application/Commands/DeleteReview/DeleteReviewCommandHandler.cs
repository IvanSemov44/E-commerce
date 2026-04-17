namespace ECommerce.Reviews.Application.Commands.DeleteReview;

public class DeleteReviewCommandHandler(
    IReviewRepository reviewRepository) : IRequestHandler<DeleteReviewCommand, Result>
{
    public async Task<Result> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result.Fail(ReviewsErrors.ReviewNotFound);

        if (!request.IsAdmin && review.UserId != request.UserId)
            return Result.Fail(ReviewsErrors.Unauthorized);

        review.Delete();

        return Result.Ok();
    }
}
