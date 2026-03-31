using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.Login;

public class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IJwtTokenService jwt,
    IUnitOfWork uow
) : IRequestHandler<LoginCommand, Result<AuthTokenDto>>
{
    public async Task<Result<AuthTokenDto>> Handle(LoginCommand command, CancellationToken ct)
    {
        // Parse email (normalize) — use same error for invalid format to avoid revealing info
        var emailResult = Email.Create(command.Email);
        if (!emailResult.IsSuccess)
            return Result<AuthTokenDto>.Fail(IdentityErrors.InvalidCredentials);

        var user = await users.GetByEmailAsync(emailResult.GetDataOrThrow(), ct);
        if (user is null)
            return Result<AuthTokenDto>.Fail(IdentityErrors.InvalidCredentials);

        if (!hasher.Verify(command.Password, user.PasswordHash.Hash))
            return Result<AuthTokenDto>.Fail(IdentityErrors.InvalidCredentials);

        var accessToken = jwt.GenerateAccessToken(user);
        var rawRefresh = jwt.GenerateRefreshToken();
        user.AddRefreshToken(rawRefresh, DateTime.UtcNow.AddDays(30));
        await users.UpdateAsync(user, ct);
        await uow.SaveChangesAsync(ct);

        return Result<AuthTokenDto>.Ok(new AuthTokenDto(accessToken, rawRefresh, user.Id));
    }
}
