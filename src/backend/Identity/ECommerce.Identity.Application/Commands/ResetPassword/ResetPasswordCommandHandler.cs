using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.ValueObjects;

namespace ECommerce.Identity.Application.Commands.ResetPassword;

public class ResetPasswordCommandHandler(
    IUserRepository users,
    IPasswordHasher hasher
) : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken ct)
    {
        var emailResult = Email.Create(command.Email);
        if (!emailResult.IsSuccess)
            return Result.Fail(IdentityErrors.InvalidCredentials);

        var user = await users.GetByEmailAsync(emailResult.GetDataOrThrow(), ct);
        if (user is null)
            return Result.Fail(IdentityErrors.InvalidCredentials);

        if (!user.IsPasswordResetTokenValid(command.Token))
            return Result.Fail(IdentityErrors.TokenInvalid);

        return user.ResetPassword(command.NewPassword, hasher);
    }
}
