namespace ECommerce.Reviews.Application.Commands.ApproveReview;

public class ApproveReviewCommandHandler(
    IReviewRepository reviewRepository) : IRequestHandler<ApproveReviewCommand, Result>
{
    public async Task<Result> Handle(ApproveReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result.Fail(ReviewsErrors.ReviewNotFound);

        return review.Approve();
    }
}
