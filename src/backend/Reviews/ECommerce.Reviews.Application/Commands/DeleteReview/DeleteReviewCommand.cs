using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Commands;

public record DeleteReviewCommand(Guid ReviewId, Guid UserId, bool IsAdmin) : IRequest<Result>, ITransactionalCommand;
