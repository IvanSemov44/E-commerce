using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Application.Extensions;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.AddAddress;

public class AddAddressCommandHandler(
    IUserRepository users,
    IUnitOfWork uow
) : IRequestHandler<AddAddressCommand, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(AddAddressCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(command.UserId, ct);
        if (user is null) return Result<UserProfileDto>.Fail(IdentityApplicationErrors.UserNotFound);

        var result = user.AddAddress(command.Street, command.City, command.Country, command.PostalCode);
        if (!result.IsSuccess) return Result<UserProfileDto>.Fail(result.GetErrorOrThrow());

        await users.UpdateAsync(user, ct);
        await uow.SaveChangesAsync(ct);
        return Result<UserProfileDto>.Ok(user.ToProfileDto());
    }
}
