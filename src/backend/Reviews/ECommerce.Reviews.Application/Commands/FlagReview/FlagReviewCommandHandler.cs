namespace ECommerce.Reviews.Application.Commands.FlagReview;

public class FlagReviewCommandHandler(
    IReviewRepository reviewRepository) : IRequestHandler<FlagReviewCommand, Result>
{
    public async Task<Result> Handle(FlagReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result.Fail(ReviewsErrors.ReviewNotFound);

        review.Flag();
        return Result.Ok();
    }
}
