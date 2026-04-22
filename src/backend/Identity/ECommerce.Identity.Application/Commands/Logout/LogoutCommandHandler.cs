namespace ECommerce.Identity.Application.Commands.Logout;

public class LogoutCommandHandler(
    IUserRepository users
) : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(command.UserId, ct);
        if (user is null) return Result.Fail(IdentityApplicationErrors.UserNotFound);

        // Idempotent: silently succeed even if token not found
        user.RevokeRefreshToken(command.RefreshToken);
        return Result.Ok();
    }
}
