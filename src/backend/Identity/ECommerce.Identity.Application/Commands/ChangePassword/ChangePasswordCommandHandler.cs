using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.ChangePassword;

public class ChangePasswordCommandHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IUnitOfWork uow
) : IRequestHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(command.UserId, ct);
        if (user is null) return Result.Fail(IdentityApplicationErrors.UserNotFound);

        if (!hasher.Verify(command.OldPassword, user.PasswordHash.Hash))
            return Result.Fail(IdentityErrors.InvalidCredentials);

        var pwValidation = PasswordHash.ValidateRawPassword(command.NewPassword);
        if (!pwValidation.IsSuccess) return pwValidation;

        var hash = hasher.Hash(command.NewPassword);
        var hashResult = PasswordHash.FromHash(hash);
        if (!hashResult.IsSuccess) return Result.Fail(hashResult.GetErrorOrThrow());

        user.ChangePassword(hashResult.GetDataOrThrow());
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
