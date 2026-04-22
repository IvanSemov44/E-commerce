namespace ECommerce.Identity.Application.Commands.DeleteAccount;

public record DeleteAccountCommand(Guid UserId) : IRequest<Result>, ITransactionalCommand;
