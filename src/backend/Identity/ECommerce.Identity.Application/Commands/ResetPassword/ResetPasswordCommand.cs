using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.ResetPassword;

public record ResetPasswordCommand(string Email, string Token, string NewPassword)
    : IRequest<Result>, ITransactionalCommand;
