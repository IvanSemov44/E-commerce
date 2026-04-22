namespace ECommerce.Identity.Application.Commands.RefreshToken;

public record RefreshTokenCommand(string Token) : IRequest<Result<AuthTokenDto>>, ITransactionalCommand;
