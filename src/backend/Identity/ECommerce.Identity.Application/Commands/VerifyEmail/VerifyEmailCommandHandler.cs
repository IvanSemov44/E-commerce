using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.VerifyEmail;

public class VerifyEmailCommandHandler(
    IUserRepository users,
    IUnitOfWork uow
) : IRequestHandler<VerifyEmailCommand, Result>
{
    public async Task<Result> Handle(VerifyEmailCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(command.UserId, ct);
        if (user is null) return Result.Fail(IdentityApplicationErrors.UserNotFound);

        var result = user.VerifyEmail(command.Token);
        if (!result.IsSuccess) return result;

        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
