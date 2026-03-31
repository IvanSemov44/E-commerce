using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Aggregates.User;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.Register;

public class RegisterCommandHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IJwtTokenService jwt,
    IUnitOfWork uow
) : IRequestHandler<RegisterCommand, Result<AuthTokenDto>>
{
    public async Task<Result<AuthTokenDto>> Handle(RegisterCommand command, CancellationToken ct)
    {
        // 1. Domain password policy check (fail fast — no repo call needed)
        var pwValidation = PasswordHash.ValidateRawPassword(command.Password);
        if (!pwValidation.IsSuccess) return Result<AuthTokenDto>.Fail(pwValidation.GetErrorOrThrow());

        // 2. Uniqueness check (requires repo — application-layer error)
        if (await users.EmailExistsAsync(command.Email, ct))
            return Result<AuthTokenDto>.Fail(IdentityApplicationErrors.EmailTaken);

        // 3. Hash (infrastructure) and wrap in domain VO
        var hash = hasher.Hash(command.Password);
        var hashResult = PasswordHash.FromHash(hash);
        if (!hashResult.IsSuccess) return Result<AuthTokenDto>.Fail(hashResult.GetErrorOrThrow());

        // 4. Create aggregate — aggregate is the sole guardian of invariants.
        //    User.Register() creates and validates Email, PersonName internally.
        var userResult = User.Register(command.Email, command.FirstName, command.LastName, hashResult.GetDataOrThrow());
        if (!userResult.IsSuccess) return Result<AuthTokenDto>.Fail(userResult.GetErrorOrThrow());

        var user = userResult.GetDataOrThrow();

        // 5. Generate tokens and attach refresh token to aggregate before persisting
        var accessToken = jwt.GenerateAccessToken(user);
        var rawRefresh  = jwt.GenerateRefreshToken();
        user.AddRefreshToken(rawRefresh, DateTime.UtcNow.AddDays(30));

        // 6. Persist user + refresh token in one save
        await users.AddAsync(user, ct);
        await uow.SaveChangesAsync(ct);

        return Result<AuthTokenDto>.Ok(new AuthTokenDto(accessToken, rawRefresh, user.Id));
    }
}
