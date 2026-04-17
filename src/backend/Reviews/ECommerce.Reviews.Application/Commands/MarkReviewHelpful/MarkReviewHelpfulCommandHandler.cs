namespace ECommerce.Reviews.Application.Commands.MarkReviewHelpful;

public class MarkReviewHelpfulCommandHandler(
    IReviewRepository reviewRepository) : IRequestHandler<MarkReviewHelpfulCommand, Result>
{
    public async Task<Result> Handle(MarkReviewHelpfulCommand request, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result.Fail(ReviewsErrors.ReviewNotFound);

        review.MarkAsHelpful();
        return Result.Ok();
    }
}
