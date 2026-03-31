using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.DeleteAccount;

public record DeleteAccountCommand(Guid UserId) : IRequest<Result>, ITransactionalCommand;
