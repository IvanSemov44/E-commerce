namespace ECommerce.Reviews.Application.Queries;

public record GetAllReviewsQuery(
    int Page,
    int PageSize,
    string? Search,
    string? Status) : IRequest<Result<PaginatedResult<ReviewDto>>>;