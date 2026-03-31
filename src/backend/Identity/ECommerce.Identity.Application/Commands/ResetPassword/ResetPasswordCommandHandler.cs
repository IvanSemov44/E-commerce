using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.ResetPassword;

public class ResetPasswordCommandHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IUnitOfWork uow
) : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken ct)
    {
        var emailResult = Email.Create(command.Email);
        if (!emailResult.IsSuccess) return Result.Fail(IdentityErrors.InvalidCredentials);

        var user = await users.GetByEmailAsync(emailResult.GetDataOrThrow(), ct);
        if (user is null) return Result.Fail(IdentityErrors.InvalidCredentials);

        if (!user.IsPasswordResetTokenValid(command.Token))
            return Result.Fail(IdentityErrors.TokenInvalid);

        var pwValidation = PasswordHash.ValidateRawPassword(command.NewPassword);
        if (!pwValidation.IsSuccess) return pwValidation;

        var hash = hasher.Hash(command.NewPassword);
        var hashResult = PasswordHash.FromHash(hash);
        if (!hashResult.IsSuccess) return Result.Fail(hashResult.GetErrorOrThrow());

        user.ChangePassword(hashResult.GetDataOrThrow());
        user.ClearPasswordResetToken();
        await users.UpdateAsync(user, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
