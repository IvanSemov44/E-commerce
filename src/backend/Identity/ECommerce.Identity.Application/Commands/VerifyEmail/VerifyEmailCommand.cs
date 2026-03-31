using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.VerifyEmail;

public record VerifyEmailCommand(Guid UserId, string Token) : IRequest<Result>, ITransactionalCommand;
