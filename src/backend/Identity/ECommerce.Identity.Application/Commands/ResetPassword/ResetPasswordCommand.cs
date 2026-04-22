namespace ECommerce.Identity.Application.Commands.ResetPassword;

public record ResetPasswordCommand(string Email, string Token, string NewPassword)
    : IRequest<Result>, ITransactionalCommand;
