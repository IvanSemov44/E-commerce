using ECommerce.Identity.Application.Extensions;

namespace ECommerce.Identity.Application.Commands.AddAddress;

public class AddAddressCommandHandler(
    IUserRepository users
) : IRequestHandler<AddAddressCommand, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(AddAddressCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(command.UserId, ct);
        if (user is null) return Result<UserProfileDto>.Fail(IdentityApplicationErrors.UserNotFound);

        var result = user.AddAddress(command.Street, command.City, command.Country, command.PostalCode);
        if (!result.IsSuccess) return Result<UserProfileDto>.Fail(result.GetErrorOrThrow());

        return Result<UserProfileDto>.Ok(user.ToProfileDto());
    }
}
