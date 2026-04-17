namespace ECommerce.Reviews.Application.QueryHandlers;

public class GetReviewByIdQueryHandler(
    IReviewRepository reviewRepository) : IRequestHandler<GetReviewByIdQuery, Result<ReviewDetailDto>>
{
    public async Task<Result<ReviewDetailDto>> Handle(GetReviewByIdQuery request, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result<ReviewDetailDto>.Fail(ReviewsErrors.ReviewNotFound);

        return Result<ReviewDetailDto>.Ok(review.ToDetailDto());
    }
}
