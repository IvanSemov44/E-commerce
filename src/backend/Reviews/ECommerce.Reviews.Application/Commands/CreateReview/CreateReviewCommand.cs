namespace ECommerce.Reviews.Application.Commands.CreateReview;

public record CreateReviewCommand(
    Guid ProductId,
    Guid UserId,
    int Rating,
    string? Title,
    string Comment) : IRequest<Result<ReviewDetailDto>>, ITransactionalCommand;
