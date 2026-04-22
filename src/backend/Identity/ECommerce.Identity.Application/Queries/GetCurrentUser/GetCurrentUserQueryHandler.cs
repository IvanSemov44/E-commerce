using ECommerce.Identity.Application.Extensions;

namespace ECommerce.Identity.Application.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler(IUserRepository users)
    : IRequestHandler<GetCurrentUserQuery, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(GetCurrentUserQuery query, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(query.UserId, ct);
        if (user is null) return Result<UserProfileDto>.Fail(IdentityApplicationErrors.UserNotFound);
        return Result<UserProfileDto>.Ok(user.ToProfileDto());
    }
}
