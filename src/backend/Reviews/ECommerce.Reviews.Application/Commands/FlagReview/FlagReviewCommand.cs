namespace ECommerce.Reviews.Application.Commands.FlagReview;

public record FlagReviewCommand(Guid ReviewId, string? Reason) : IRequest<Result>, ITransactionalCommand;
