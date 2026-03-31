using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.DeleteAccount;

public class DeleteAccountCommandHandler(
    IUserRepository users,
    IUnitOfWork uow
) : IRequestHandler<DeleteAccountCommand, Result>
{
    public async Task<Result> Handle(DeleteAccountCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(command.UserId, ct);
        if (user is null) return Result.Fail(IdentityApplicationErrors.UserNotFound);

        await users.DeleteAsync(user, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
