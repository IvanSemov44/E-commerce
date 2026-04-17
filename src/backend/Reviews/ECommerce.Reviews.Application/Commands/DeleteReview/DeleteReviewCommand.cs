namespace ECommerce.Reviews.Application.Commands.DeleteReview;

public record DeleteReviewCommand(Guid ReviewId, Guid UserId, bool IsAdmin) : IRequest<Result>, ITransactionalCommand;
