namespace ECommerce.Identity.Application.Commands.DeleteAccount;

public class DeleteAccountCommandHandler(
    IUserRepository users
) : IRequestHandler<DeleteAccountCommand, Result>
{
    public async Task<Result> Handle(DeleteAccountCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(command.UserId, ct);
        if (user is null)
            return Result.Fail(IdentityApplicationErrors.UserNotFound);

        user.DeleteAccount();
        return Result.Ok();
    }
}
