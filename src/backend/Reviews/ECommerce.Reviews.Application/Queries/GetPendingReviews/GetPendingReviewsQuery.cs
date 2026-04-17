namespace ECommerce.Reviews.Application.Queries;

public record GetPendingReviewsQuery(
    int Page,
    int PageSize) : IRequest<Result<PaginatedResult<ReviewDetailDto>>>;