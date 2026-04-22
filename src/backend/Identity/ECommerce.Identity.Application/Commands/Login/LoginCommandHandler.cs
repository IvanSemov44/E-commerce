using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.ValueObjects;

namespace ECommerce.Identity.Application.Commands.Login;

public class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IJwtTokenService jwt
) : IRequestHandler<LoginCommand, Result<AuthTokenDto>>
{
    public async Task<Result<AuthTokenDto>> Handle(LoginCommand command, CancellationToken ct)
    {
        var emailResult = Email.Create(command.Email);
        if (!emailResult.IsSuccess)
            return Result<AuthTokenDto>.Fail(IdentityErrors.InvalidCredentials);

        var user = await users.GetByEmailAsync(emailResult.GetDataOrThrow(), ct);
        if (user is null)
            return Result<AuthTokenDto>.Fail(IdentityErrors.InvalidCredentials);

        if (!user.VerifyPassword(command.Password, hasher))
            return Result<AuthTokenDto>.Fail(IdentityErrors.InvalidCredentials);

        var accessToken = jwt.GenerateAccessToken(user);
        var rawRefresh = jwt.GenerateRefreshToken();
        user.AddRefreshToken(rawRefresh, DateTime.UtcNow.AddDays(30));

        return Result<AuthTokenDto>.Ok(new AuthTokenDto(accessToken, rawRefresh, user.Id));
    }
}
