using ECommerce.Reviews.Domain.Errors;
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Reviews.Application.CommandHandlers;

public class RejectReviewCommandHandler(
    IReviewRepository reviewRepository) : IRequestHandler<RejectReviewCommand, Result>
{
    public async Task<Result> Handle(RejectReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result.Fail(ReviewsErrors.ReviewNotFound);

        review.Reject(DateTime.UtcNow);
        await reviewRepository.UpsertAsync(review, cancellationToken);

        return Result.Ok();
    }
}
