using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Application.Extensions;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.UpdateProfile;

public class UpdateProfileCommandHandler(
    IUserRepository users,
    IUnitOfWork uow
) : IRequestHandler<UpdateProfileCommand, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(UpdateProfileCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(command.UserId, ct);
        if (user is null) return Result<UserProfileDto>.Fail(IdentityApplicationErrors.UserNotFound);

        var nameResult = PersonName.Create(command.FirstName, command.LastName);
        if (!nameResult.IsSuccess) return Result<UserProfileDto>.Fail(nameResult.GetErrorOrThrow());

        user.UpdateProfile(nameResult.GetDataOrThrow(), command.PhoneNumber);
        await users.UpdateAsync(user, ct);
        await uow.SaveChangesAsync(ct);
        return Result<UserProfileDto>.Ok(user.ToProfileDto());
    }
}
