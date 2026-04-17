namespace ECommerce.Reviews.Application.Commands.UpdateReview;

public record UpdateReviewCommand(
    Guid ReviewId,
    Guid UserId,
    bool IsAdmin,
    int? Rating,
    string? Title,
    string? Comment) : IRequest<Result>, ITransactionalCommand;
