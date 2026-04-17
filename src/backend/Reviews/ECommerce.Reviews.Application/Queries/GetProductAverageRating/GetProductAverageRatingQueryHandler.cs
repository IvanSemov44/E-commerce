namespace ECommerce.Reviews.Application.QueryHandlers;

public class GetProductAverageRatingQueryHandler(
    IReviewRepository reviewRepository,
    IProductProjectionService catalogService) : IRequestHandler<GetProductAverageRatingQuery, Result<decimal>>
{
    public async Task<Result<decimal>> Handle(GetProductAverageRatingQuery request, CancellationToken cancellationToken)
    {
        if (!await catalogService.ProductExistsAsync(request.ProductId, cancellationToken))
            return Result<decimal>.Fail(ReviewsErrors.ProductNotFound);

        decimal averageRating = await reviewRepository.GetAverageRatingAsync(request.ProductId, cancellationToken);
        return Result<decimal>.Ok(averageRating);
    }
}
