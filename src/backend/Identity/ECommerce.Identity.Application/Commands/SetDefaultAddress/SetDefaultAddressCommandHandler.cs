using ECommerce.Identity.Application.Extensions;

namespace ECommerce.Identity.Application.Commands.SetDefaultAddress;

public class SetDefaultAddressCommandHandler(
    IUserRepository users
) : IRequestHandler<SetDefaultAddressCommand, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(SetDefaultAddressCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(command.UserId, ct);
        if (user is null)
            return Result<UserProfileDto>.Fail(IdentityApplicationErrors.UserNotFound);

        var result = user.SetDefaultShippingAddress(command.AddressId);
        if (!result.IsSuccess)
            return Result<UserProfileDto>.Fail(result.GetErrorOrThrow());

        return Result<UserProfileDto>.Ok(user.ToProfileDto());
    }
}
