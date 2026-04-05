using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Commands;

public record FlagReviewCommand(Guid ReviewId, string? Reason) : IRequest<Result>, ITransactionalCommand;