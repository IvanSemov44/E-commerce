namespace ECommerce.Identity.Application.Commands.ChangePassword;

public class ChangePasswordCommandHandler(
    IUserRepository users,
    IPasswordHasher hasher
) : IRequestHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(command.UserId, ct);
        if (user is null)
            return Result.Fail(IdentityApplicationErrors.UserNotFound);

        return user.ChangePassword(command.OldPassword, command.NewPassword, hasher);
    }
}
