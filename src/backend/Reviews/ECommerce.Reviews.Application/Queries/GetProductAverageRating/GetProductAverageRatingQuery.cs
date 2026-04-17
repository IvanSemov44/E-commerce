namespace ECommerce.Reviews.Application.Queries;

public record GetProductAverageRatingQuery(Guid ProductId) : IRequest<Result<decimal>>;