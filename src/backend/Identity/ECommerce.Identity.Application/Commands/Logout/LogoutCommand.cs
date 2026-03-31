using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.Logout;

public record LogoutCommand(Guid UserId, string RefreshToken) : IRequest<Result>, ITransactionalCommand;
