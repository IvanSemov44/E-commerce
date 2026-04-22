using ECommerce.Identity.Domain.Aggregates.User;

namespace ECommerce.Identity.Application.Commands.Register;

public class RegisterCommandHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IJwtTokenService jwt
) : IRequestHandler<RegisterCommand, Result<AuthTokenDto>>
{
    public async Task<Result<AuthTokenDto>> Handle(RegisterCommand command, CancellationToken ct)
    {
        if (await users.EmailExistsAsync(command.Email, ct))
            return Result<AuthTokenDto>.Fail(IdentityApplicationErrors.EmailTaken);

        var userResult = User.Register(command.Email, command.FirstName, command.LastName, command.Password, hasher);
        if (!userResult.IsSuccess)
            return Result<AuthTokenDto>.Fail(userResult.GetErrorOrThrow());

        var user = userResult.GetDataOrThrow();
        var accessToken = jwt.GenerateAccessToken(user);
        var rawRefresh = jwt.GenerateRefreshToken();
        user.AddRefreshToken(rawRefresh, DateTime.UtcNow.AddDays(30));

        await users.AddAsync(user, ct);

        return Result<AuthTokenDto>.Ok(new AuthTokenDto(accessToken, rawRefresh, user.Id));
    }
}
