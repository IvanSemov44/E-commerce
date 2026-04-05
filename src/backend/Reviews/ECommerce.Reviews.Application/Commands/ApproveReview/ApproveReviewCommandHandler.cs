using ECommerce.Reviews.Domain.Errors;
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Reviews.Application.CommandHandlers;

public class ApproveReviewCommandHandler(
    IReviewRepository reviewRepository,
    ECommerce.SharedKernel.Interfaces.IUnitOfWork unitOfWork) : IRequestHandler<ApproveReviewCommand, Result>
{
    public async Task<Result> Handle(ApproveReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result.Fail(ReviewsErrors.ReviewNotFound);

        Result approveResult = review.Approve(DateTime.UtcNow);
        if (!approveResult.IsSuccess)
            return approveResult;

        await reviewRepository.UpsertAsync(review, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}