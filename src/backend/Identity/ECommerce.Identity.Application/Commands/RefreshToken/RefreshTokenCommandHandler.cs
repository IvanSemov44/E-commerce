using ECommerce.Identity.Domain.Errors;

namespace ECommerce.Identity.Application.Commands.RefreshToken;

public class RefreshTokenCommandHandler(
    IUserRepository users,
    IJwtTokenService jwt
) : IRequestHandler<RefreshTokenCommand, Result<AuthTokenDto>>
{
    public async Task<Result<AuthTokenDto>> Handle(RefreshTokenCommand command, CancellationToken ct)
    {
        var user = await users.GetByRefreshTokenAsync(command.Token, ct);
        if (user is null)
            return Result<AuthTokenDto>.Fail(IdentityErrors.TokenInvalid);

        var rotateResult = user.RotateRefreshToken(
            command.Token,
            jwt.GenerateRefreshToken(),
            DateTime.UtcNow.AddDays(30));

        if (!rotateResult.IsSuccess)
            return Result<AuthTokenDto>.Fail(rotateResult.GetErrorOrThrow());

        var accessToken = jwt.GenerateAccessToken(user);
        return Result<AuthTokenDto>.Ok(new AuthTokenDto(accessToken, rotateResult.GetDataOrThrow(), user.Id));
    }
}
