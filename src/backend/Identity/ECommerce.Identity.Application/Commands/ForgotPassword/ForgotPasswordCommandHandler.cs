using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler(
    IUserRepository users,
    IUnitOfWork uow
) : IRequestHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken ct)
    {
        var emailResult = Email.Create(command.Email);
        if (!emailResult.IsSuccess) return Result.Ok(); // Don't reveal if email exists

        var user = await users.GetByEmailAsync(emailResult.GetDataOrThrow(), ct);
        if (user is null) return Result.Ok(); // Same response whether found or not

        var token = Guid.NewGuid().ToString("N");
        user.SetPasswordResetToken(token, DateTime.UtcNow.AddHours(1));
        await users.UpdateAsync(user, ct);
        await uow.SaveChangesAsync(ct);

        // TODO: raise PasswordResetRequestedEvent to trigger email via event handler
        return Result.Ok();
    }
}
