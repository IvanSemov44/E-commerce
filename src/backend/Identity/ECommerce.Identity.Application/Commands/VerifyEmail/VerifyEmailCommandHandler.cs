namespace ECommerce.Identity.Application.Commands.VerifyEmail;

public class VerifyEmailCommandHandler(
    IUserRepository users
) : IRequestHandler<VerifyEmailCommand, Result>
{
    public async Task<Result> Handle(VerifyEmailCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(command.UserId, ct);
        if (user is null)
            return Result.Fail(IdentityApplicationErrors.UserNotFound);

        return user.VerifyEmail(command.Token);
    }
}
