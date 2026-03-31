using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.RefreshToken;

public class RefreshTokenCommandHandler(
    IUserRepository users,
    IJwtTokenService jwt,
    IUnitOfWork uow
) : IRequestHandler<RefreshTokenCommand, Result<AuthTokenDto>>
{
    public async Task<Result<AuthTokenDto>> Handle(RefreshTokenCommand command, CancellationToken ct)
    {
        var user = await users.GetByRefreshTokenAsync(command.Token, ct);
        if (user is null)
            return Result<AuthTokenDto>.Fail(IdentityErrors.TokenInvalid);

        var existing = user.GetActiveRefreshToken(command.Token);
        if (existing is null)
            return Result<AuthTokenDto>.Fail(IdentityErrors.TokenRevoked);

        // Rotate: revoke old token via aggregate, issue new one
        user.RevokeRefreshToken(command.Token);
        var accessToken = jwt.GenerateAccessToken(user);
        var rawRefresh = jwt.GenerateRefreshToken();
        user.AddRefreshToken(rawRefresh, DateTime.UtcNow.AddDays(30));
        await users.UpdateAsync(user, ct);
        await uow.SaveChangesAsync(ct);

        return Result<AuthTokenDto>.Ok(new AuthTokenDto(accessToken, rawRefresh, user.Id));
    }
}
