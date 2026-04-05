using ECommerce.Reviews.Domain.Errors;
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Reviews.Application.CommandHandlers;

public class MarkReviewHelpfulCommandHandler(
    IReviewRepository reviewRepository,
    ECommerce.SharedKernel.Interfaces.IUnitOfWork unitOfWork) : IRequestHandler<MarkReviewHelpfulCommand, Result>
{
    public async Task<Result> Handle(MarkReviewHelpfulCommand request, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result.Fail(ReviewsErrors.ReviewNotFound);

        review.MarkAsHelpful(DateTime.UtcNow);
        await reviewRepository.UpsertAsync(review, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}