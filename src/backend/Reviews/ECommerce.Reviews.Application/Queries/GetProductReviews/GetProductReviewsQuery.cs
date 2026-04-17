namespace ECommerce.Reviews.Application.Queries;

public record GetProductReviewsQuery(
    Guid ProductId,
    int Page,
    int PageSize) : IRequest<Result<PaginatedResult<ReviewDto>>>;