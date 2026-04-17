namespace ECommerce.Reviews.Application.Queries;

public record GetReviewByIdQuery(Guid ReviewId) : IRequest<Result<ReviewDetailDto>>;