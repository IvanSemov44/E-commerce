namespace ECommerce.Reviews.Application.QueryHandlers;

public class GetPendingReviewsQueryHandler(
    IReviewRepository reviewRepository) : IRequestHandler<GetPendingReviewsQuery, Result<PaginatedResult<ReviewDetailDto>>>
{
    public async Task<Result<PaginatedResult<ReviewDetailDto>>> Handle(GetPendingReviewsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await reviewRepository.GetPendingAsync(request.Page, request.PageSize, cancellationToken);

        return Result<PaginatedResult<ReviewDetailDto>>.Ok(new PaginatedResult<ReviewDetailDto>
        {
            Items = items.Select(review => review.ToDetailDto()).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
}
