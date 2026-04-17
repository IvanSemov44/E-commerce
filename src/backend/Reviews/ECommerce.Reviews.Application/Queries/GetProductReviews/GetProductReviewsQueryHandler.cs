namespace ECommerce.Reviews.Application.QueryHandlers;

public class GetProductReviewsQueryHandler(
    IReviewRepository reviewRepository,
    IProductProjectionService catalogService) : IRequestHandler<GetProductReviewsQuery, Result<PaginatedResult<ReviewDto>>>
{
    public async Task<Result<PaginatedResult<ReviewDto>>> Handle(GetProductReviewsQuery request, CancellationToken cancellationToken)
    {
        if (!await catalogService.ProductExistsAsync(request.ProductId, cancellationToken))
            return Result<PaginatedResult<ReviewDto>>.Fail(ReviewsErrors.ProductNotFound);

        var (items, totalCount) = await reviewRepository.GetByProductAsync(request.ProductId, request.Page, request.PageSize, true, cancellationToken);

        return Result<PaginatedResult<ReviewDto>>.Ok(new PaginatedResult<ReviewDto>
        {
            Items = items.Select(review => review.ToDto()).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
}
