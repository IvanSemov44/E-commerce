namespace ECommerce.Reviews.Application.QueryHandlers;

public class GetAllReviewsQueryHandler(
    IReviewRepository reviewRepository) : IRequestHandler<GetAllReviewsQuery, Result<PaginatedResult<ReviewDto>>>
{
    public async Task<Result<PaginatedResult<ReviewDto>>> Handle(GetAllReviewsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await reviewRepository.GetAllAsync(request.Page, request.PageSize, request.Search, request.Status, cancellationToken);

        return Result<PaginatedResult<ReviewDto>>.Ok(new PaginatedResult<ReviewDto>
        {
            Items = items.Select(review => review.ToDto()).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
}
