namespace ECommerce.Reviews.Application.Commands.MarkReviewHelpful;

public record MarkReviewHelpfulCommand(Guid ReviewId) : IRequest<Result>, ITransactionalCommand;
