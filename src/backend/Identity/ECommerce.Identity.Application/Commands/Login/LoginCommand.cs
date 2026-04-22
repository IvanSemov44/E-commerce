namespace ECommerce.Identity.Application.Commands.Login;

public record LoginCommand(string Email, string Password)
    : IRequest<Result<AuthTokenDto>>, ITransactionalCommand;
