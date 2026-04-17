namespace ECommerce.Reviews.Application.Queries;

public record GetUserReviewsQuery(
    Guid UserId,
    int Page,
    int PageSize) : IRequest<Result<PaginatedResult<ReviewDetailDto>>>;