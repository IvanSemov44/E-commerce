using ECommerce.Identity.Domain.ValueObjects;

namespace ECommerce.Identity.Application.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler(
    IUserRepository users
) : IRequestHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken ct)
    {
        var emailResult = Email.Create(command.Email);
        if (!emailResult.IsSuccess)
            return Result.Ok();

        var user = await users.GetByEmailAsync(emailResult.GetDataOrThrow(), ct);
        if (user is null)
            return Result.Ok();

        user.RequestPasswordReset();

        // TODO: raise PasswordResetRequestedEvent to trigger email via event handler
        return Result.Ok();
    }
}
