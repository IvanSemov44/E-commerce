using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Commands;

public record ApproveReviewCommand(Guid ReviewId) : IRequest<Result>, ITransactionalCommand;