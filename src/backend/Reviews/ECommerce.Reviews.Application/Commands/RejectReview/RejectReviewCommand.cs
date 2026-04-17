namespace ECommerce.Reviews.Application.Commands.RejectReview;

public record RejectReviewCommand(Guid ReviewId) : IRequest<Result>, ITransactionalCommand;
