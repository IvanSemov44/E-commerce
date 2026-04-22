namespace ECommerce.Identity.Application.Commands.ChangePassword;

public record ChangePasswordCommand(Guid UserId, string OldPassword, string NewPassword)
    : IRequest<Result>, ITransactionalCommand;
