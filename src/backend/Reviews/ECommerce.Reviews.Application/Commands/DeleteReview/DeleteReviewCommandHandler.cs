using ECommerce.Reviews.Domain.Errors;
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Reviews.Application.CommandHandlers;

public class DeleteReviewCommandHandler(
    IReviewRepository reviewRepository,
    ECommerce.SharedKernel.Interfaces.IUnitOfWork unitOfWork) : IRequestHandler<DeleteReviewCommand, Result>
{
    public async Task<Result> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result.Fail(ReviewsErrors.ReviewNotFound);

        if (!request.IsAdmin && review.UserId != request.UserId)
            return Result.Fail(ReviewsErrors.Unauthorized);

        await reviewRepository.DeleteAsync(review, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
