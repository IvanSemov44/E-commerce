namespace ECommerce.Reviews.Application.Commands.ApproveReview;

public record ApproveReviewCommand(Guid ReviewId) : IRequest<Result>, ITransactionalCommand;
