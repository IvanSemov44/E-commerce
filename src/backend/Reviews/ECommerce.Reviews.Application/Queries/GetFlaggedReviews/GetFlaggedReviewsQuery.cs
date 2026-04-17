namespace ECommerce.Reviews.Application.Queries;

public record GetFlaggedReviewsQuery(
    int Page,
    int PageSize) : IRequest<Result<PaginatedResult<ReviewDto>>>;