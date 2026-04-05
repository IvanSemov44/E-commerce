using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Commands;

public record RejectReviewCommand(Guid ReviewId) : IRequest<Result>, ITransactionalCommand;