namespace ECommerce.Identity.Application.Commands.VerifyEmail;

public record VerifyEmailCommand(Guid UserId, string Token) : IRequest<Result>, ITransactionalCommand;
