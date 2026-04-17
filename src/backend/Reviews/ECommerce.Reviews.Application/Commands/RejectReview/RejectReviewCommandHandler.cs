namespace ECommerce.Reviews.Application.Commands.RejectReview;

public class RejectReviewCommandHandler(
    IReviewRepository reviewRepository) : IRequestHandler<RejectReviewCommand, Result>
{
    public async Task<Result> Handle(RejectReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result.Fail(ReviewsErrors.ReviewNotFound);

        review.Reject();
        return Result.Ok();
    }
}
