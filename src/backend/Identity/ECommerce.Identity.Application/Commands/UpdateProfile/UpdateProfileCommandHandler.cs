using ECommerce.Identity.Application.Extensions;

namespace ECommerce.Identity.Application.Commands.UpdateProfile;

public class UpdateProfileCommandHandler(
    IUserRepository users
) : IRequestHandler<UpdateProfileCommand, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(UpdateProfileCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(command.UserId, ct);
        if (user is null)
            return Result<UserProfileDto>.Fail(IdentityApplicationErrors.UserNotFound);

        var result = user.UpdateProfile(command.FirstName, command.LastName, command.PhoneNumber);
        if (!result.IsSuccess)
            return Result<UserProfileDto>.Fail(result.GetErrorOrThrow());

        return Result<UserProfileDto>.Ok(user.ToProfileDto());
    }
}
