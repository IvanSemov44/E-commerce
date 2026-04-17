namespace ECommerce.Reviews.Application.Commands.CreateReview;

public class CreateReviewCommandHandler(
    IReviewRepository reviewRepository,
    IProductProjectionService catalogService) : IRequestHandler<CreateReviewCommand, Result<ReviewDetailDto>>
{
    public async Task<Result<ReviewDetailDto>> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        if (!await catalogService.ProductExistsAsync(request.ProductId, cancellationToken))
            return Result<ReviewDetailDto>.Fail(ReviewsErrors.ProductNotFound);

        if (await reviewRepository.ExistsAsync(request.ProductId, request.UserId, cancellationToken))
            return Result<ReviewDetailDto>.Fail(ReviewsErrors.DuplicateReview);

        var ratingResult = Rating.Create(request.Rating);
        if (!ratingResult.IsSuccess)
            return Result<ReviewDetailDto>.Fail(ratingResult.GetErrorOrThrow());

        string comment = request.Comment.Trim();
        var contentResult = ReviewContent.Create(request.Title, comment);
        if (!contentResult.IsSuccess)
            return Result<ReviewDetailDto>.Fail(contentResult.GetErrorOrThrow());

        var review = Review.Create(
            request.ProductId,
            request.UserId,
            ratingResult.GetDataOrThrow(),
            contentResult.GetDataOrThrow());

        await reviewRepository.AddAsync(review, cancellationToken);

        return Result<ReviewDetailDto>.Ok(review.ToDetailDto());
    }
}
