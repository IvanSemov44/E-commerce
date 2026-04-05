using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Commands;

public record MarkReviewHelpfulCommand(Guid ReviewId) : IRequest<Result>, ITransactionalCommand;