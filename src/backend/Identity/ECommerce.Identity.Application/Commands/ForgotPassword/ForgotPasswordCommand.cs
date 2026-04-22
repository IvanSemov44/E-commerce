namespace ECommerce.Identity.Application.Commands.ForgotPassword;

/// <summary>
/// Always returns success — never reveals if email exists.
/// </summary>
public record ForgotPasswordCommand(string Email) : IRequest<Result>, ITransactionalCommand;
