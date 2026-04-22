using ECommerce.Identity.Application.Extensions;

namespace ECommerce.Identity.Application.Commands.DeleteAddress;

public class DeleteAddressCommandHandler(
    IUserRepository users
) : IRequestHandler<DeleteAddressCommand, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(DeleteAddressCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(command.UserId, ct);
        if (user is null)
            return Result<UserProfileDto>.Fail(IdentityApplicationErrors.UserNotFound);

        var result = user.DeleteAddress(command.AddressId);
        if (!result.IsSuccess)
            return Result<UserProfileDto>.Fail(result.GetErrorOrThrow());

        return Result<UserProfileDto>.Ok(user.ToProfileDto());
    }
}
