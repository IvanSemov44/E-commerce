# Phase 2, Step 2: Identity Application Project

**Prerequisite**: Step 1 (`ECommerce.Identity.Domain`) is complete and `dotnet build` passes.

---

## Task: Create ECommerce.Identity.Application Project

### 1. Create the project

```bash
cd src/backend
dotnet new classlib -n ECommerce.Identity.Application -f net10.0 -o Identity/ECommerce.Identity.Application
dotnet sln ../../ECommerce.sln add Identity/ECommerce.Identity.Application/ECommerce.Identity.Application.csproj

dotnet add Identity/ECommerce.Identity.Application/ECommerce.Identity.Application.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
dotnet add Identity/ECommerce.Identity.Application/ECommerce.Identity.Application.csproj \
    reference Identity/ECommerce.Identity.Domain/ECommerce.Identity.Domain.csproj

dotnet add Identity/ECommerce.Identity.Application/ECommerce.Identity.Application.csproj package MediatR
dotnet add Identity/ECommerce.Identity.Application/ECommerce.Identity.Application.csproj package FluentValidation

rm Identity/ECommerce.Identity.Application/Class1.cs
```

### 2. Create application errors

Application-layer errors require a repository lookup — they cannot be raised by the aggregate alone. Keep them separate from domain errors (same pattern as `CatalogApplicationErrors` in Phase 1).

**File: `Identity/ECommerce.Identity.Application/Errors/IdentityApplicationErrors.cs`**

```csharp
using ECommerce.SharedKernel.Results;

namespace ECommerce.Identity.Application.Errors;

public static class IdentityApplicationErrors
{
    // Raised by handlers after a repo lookup — not by the aggregate itself
    public static readonly DomainError UserNotFound    = new("USER_NOT_FOUND",    "User not found.");
    public static readonly DomainError EmailTaken      = new("EMAIL_TAKEN",       "This email address is already registered.");
    public static readonly DomainError AddressNotFound = new("ADDRESS_NOT_FOUND", "Address not found.");
}
```

> **Rule**: `IdentityErrors` (domain) = errors the aggregate can raise by itself (PasswordTooShort, EmailInvalid, AddressLimit, etc.).
> `IdentityApplicationErrors` (application) = errors that require a repository check (UserNotFound, EmailTaken).
> This keeps the domain free of application concerns.

---

### 3. Create application-layer interfaces

These interfaces live in Application (not Domain) because hashing and JWT are infrastructure concerns.

**File: `Identity/ECommerce.Identity.Application/Interfaces/IPasswordHasher.cs`**
```csharp
namespace ECommerce.Identity.Application.Interfaces;

public interface IPasswordHasher
{
    string Hash(string rawPassword);
    bool   Verify(string rawPassword, string hash);
}
```

**File: `Identity/ECommerce.Identity.Application/Interfaces/IJwtTokenService.cs`**
```csharp
using ECommerce.Identity.Domain.Aggregates.User;

namespace ECommerce.Identity.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
```

### 3. Create DTOs

**File: `Identity/ECommerce.Identity.Application/DTOs/AuthTokenDto.cs`**
```csharp
namespace ECommerce.Identity.Application.DTOs;

public record AuthTokenDto(
    string AccessToken,
    string RefreshToken,
    Guid   UserId
);
```

**File: `Identity/ECommerce.Identity.Application/DTOs/UserProfileDto.cs`**
```csharp
namespace ECommerce.Identity.Application.DTOs;

public record UserProfileDto(
    Guid   Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string Role,
    bool   IsEmailVerified,
    IReadOnlyList<AddressDto> Addresses
);
```

**File: `Identity/ECommerce.Identity.Application/DTOs/AddressDto.cs`**
```csharp
namespace ECommerce.Identity.Application.DTOs;

public record AddressDto(
    Guid    Id,
    string  Street,
    string  City,
    string  Country,
    string? PostalCode,
    bool    IsDefaultShipping,
    bool    IsDefaultBilling
);
```

### 4. Create commands

For each command: a folder with `Command.cs`, `CommandHandler.cs`, `CommandValidator.cs`.

---

**`Commands/Register/RegisterCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Identity.Application.DTOs;

namespace ECommerce.Identity.Application.Commands.Register;

public record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password
) : IRequest<Result<AuthTokenDto>>, ITransactionalCommand;
```

**`Commands/Register/RegisterCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Aggregates.User;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Identity.Application.Commands.Register;

public class RegisterCommandHandler(
    IUserRepository _users,
    IPasswordHasher _hasher,
    IJwtTokenService _jwt,
    IUnitOfWork _uow
) : IRequestHandler<RegisterCommand, Result<AuthTokenDto>>
{
    public async Task<Result<AuthTokenDto>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        // 1. Validate value objects
        var emailResult = Email.Create(command.Email);
        if (!emailResult.IsSuccess) return Result<AuthTokenDto>.Fail(emailResult.GetErrorOrThrow());

        var nameResult = PersonName.Create(command.FirstName, command.LastName);
        if (!nameResult.IsSuccess) return Result<AuthTokenDto>.Fail(nameResult.GetErrorOrThrow());

        // 2. Domain password policy check
        var pwValidation = PasswordHash.ValidateRawPassword(command.Password);
        if (!pwValidation.IsSuccess) return Result<AuthTokenDto>.Fail(pwValidation.GetErrorOrThrow());

        // 3. Uniqueness check (requires repo lookup — application-layer error)
        if (await _users.EmailExistsAsync(command.Email, cancellationToken))
            return Result<AuthTokenDto>.Fail(IdentityApplicationErrors.EmailTaken);

        // 4. Hash (infrastructure) and wrap in domain VO
        var hash       = _hasher.Hash(command.Password);
        var hashResult = PasswordHash.FromHash(hash);
        if (!hashResult.IsSuccess) return Result<AuthTokenDto>.Fail(hashResult.GetErrorOrThrow());

        // 5. Create aggregate (raises UserRegisteredEvent)
        var user = User.Register(emailResult.GetDataOrThrow(), nameResult.GetDataOrThrow(), hashResult.GetDataOrThrow());

        // 6. Persist
        await _users.AddAsync(user, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        // 7. Generate tokens
        var accessToken  = _jwt.GenerateAccessToken(user);
        var rawRefresh   = _jwt.GenerateRefreshToken();
        user.AddRefreshToken(rawRefresh, DateTime.UtcNow.AddDays(30));
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<AuthTokenDto>.Ok(new AuthTokenDto(accessToken, rawRefresh, user.Id));
    }
}
```

**`Commands/Register/RegisterCommandValidator.cs`**
```csharp
using FluentValidation;

namespace ECommerce.Identity.Application.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
```

---

**`Commands/Login/LoginCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Identity.Application.DTOs;

namespace ECommerce.Identity.Application.Commands.Login;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<Result<AuthTokenDto>>, ITransactionalCommand;
```

**`Commands/Login/LoginCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Identity.Application.Commands.Login;

public class LoginCommandHandler(
    IUserRepository _users,
    IPasswordHasher _hasher,
    IJwtTokenService _jwt,
    IUnitOfWork _uow
) : IRequestHandler<LoginCommand, Result<AuthTokenDto>>
{
    public async Task<Result<AuthTokenDto>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        // Parse email (normalize)
        var emailResult = Email.Create(command.Email);
        if (!emailResult.IsSuccess)
            return Result<AuthTokenDto>.Fail(IdentityErrors.InvalidCredentials);  // don't reveal why

        var user = await _users.GetByEmailAsync(emailResult.GetDataOrThrow(), cancellationToken);
        if (user is null)
            return Result<AuthTokenDto>.Fail(IdentityErrors.InvalidCredentials);  // same error — don't reveal if email exists

        if (!_hasher.Verify(command.Password, user.PasswordHash.Hash))
            return Result<AuthTokenDto>.Fail(IdentityErrors.InvalidCredentials);

        var accessToken = _jwt.GenerateAccessToken(user);
        var rawRefresh  = _jwt.GenerateRefreshToken();
        user.AddRefreshToken(rawRefresh, DateTime.UtcNow.AddDays(30));
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<AuthTokenDto>.Ok(new AuthTokenDto(accessToken, rawRefresh, user.Id));
    }
}
```

**`Commands/Login/LoginCommandValidator.cs`**
```csharp
using FluentValidation;

namespace ECommerce.Identity.Application.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
```

---

**`Commands/RefreshToken/RefreshTokenCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Identity.Application.DTOs;

namespace ECommerce.Identity.Application.Commands.RefreshToken;

public record RefreshTokenCommand(string Token) : IRequest<Result<AuthTokenDto>>, ITransactionalCommand;
```

**`Commands/RefreshToken/RefreshTokenCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Identity.Application.Commands.RefreshToken;

public class RefreshTokenCommandHandler(
    IUserRepository _users,
    IJwtTokenService _jwt,
    IUnitOfWork _uow
) : IRequestHandler<RefreshTokenCommand, Result<AuthTokenDto>>
{
    public async Task<Result<AuthTokenDto>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        // NOTE: This requires finding user by refresh token.
        // Add GetByRefreshTokenAsync to IUserRepository in Step 3 if needed,
        // or query via the token string. For now, add this method to the interface.
        var user = await _users.GetByRefreshTokenAsync(command.Token, cancellationToken);
        if (user is null)
            return Result<AuthTokenDto>.Fail(IdentityErrors.TokenInvalid);

        var existing = user.GetActiveRefreshToken(command.Token);
        if (existing is null)
            return Result<AuthTokenDto>.Fail(IdentityErrors.TokenRevoked);

        existing.Revoke("Rotated");
        var accessToken = _jwt.GenerateAccessToken(user);
        var rawRefresh  = _jwt.GenerateRefreshToken();
        user.AddRefreshToken(rawRefresh, DateTime.UtcNow.AddDays(30));
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<AuthTokenDto>.Ok(new AuthTokenDto(accessToken, rawRefresh, user.Id));
    }
}
```

> After writing this handler, add `Task<User?> GetByRefreshTokenAsync(string token, CancellationToken ct = default)` to `IUserRepository`.

**`Commands/RefreshToken/RefreshTokenCommandValidator.cs`**
```csharp
using FluentValidation;

namespace ECommerce.Identity.Application.Commands.RefreshToken;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator() => RuleFor(x => x.Token).NotEmpty();
}
```

---

**`Commands/VerifyEmail/VerifyEmailCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Identity.Application.Commands.VerifyEmail;

public record VerifyEmailCommand(Guid UserId, string Token) : IRequest<Result>, ITransactionalCommand;
```

**`Commands/VerifyEmail/VerifyEmailCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Identity.Application.Commands.VerifyEmail;

public class VerifyEmailCommandHandler(
    IUserRepository _users,
    IUnitOfWork _uow
) : IRequestHandler<VerifyEmailCommand, Result>
{
    public async Task<Result> Handle(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null) return Result.Fail(IdentityApplicationErrors.UserNotFound);

        var result = user.VerifyEmail(command.Token);
        if (!result.IsSuccess) return result;

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
```

**`Commands/VerifyEmail/VerifyEmailCommandValidator.cs`**
```csharp
using FluentValidation;

namespace ECommerce.Identity.Application.Commands.VerifyEmail;

public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Token).NotEmpty();
    }
}
```

---

**`Commands/ForgotPassword/ForgotPasswordCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Identity.Application.Commands.ForgotPassword;

// No ITransactionalCommand — we store a reset token and send email.
// Email sending is fire-and-forget; token storage needs SaveChanges.
public record ForgotPasswordCommand(string Email) : IRequest<Result>, ITransactionalCommand;
```

**`Commands/ForgotPassword/ForgotPasswordCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Identity.Application.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler(
    IUserRepository _users,
    IUnitOfWork _uow
) : IRequestHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(command.Email);
        if (!emailResult.IsSuccess) return Result.Ok(); // Don't reveal if email exists

        var user = await _users.GetByEmailAsync(emailResult.GetDataOrThrow(), cancellationToken);
        if (user is null) return Result.Ok(); // Same response whether found or not

        var token = Guid.NewGuid().ToString("N");
        user.SetPasswordResetToken(token, DateTime.UtcNow.AddHours(1));
        await _uow.SaveChangesAsync(cancellationToken);

        // TODO: raise PasswordResetRequestedEvent to trigger email via event handler
        // For now: fire-and-forget email can be sent here or via domain event

        return Result.Ok();
    }
}
```

**`Commands/ForgotPassword/ForgotPasswordCommandValidator.cs`**
```csharp
using FluentValidation;

namespace ECommerce.Identity.Application.Commands.ForgotPassword;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator() => RuleFor(x => x.Email).NotEmpty();
}
```

---

**`Commands/ResetPassword/ResetPasswordCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Identity.Application.Commands.ResetPassword;

public record ResetPasswordCommand(string Email, string Token, string NewPassword)
    : IRequest<Result>, ITransactionalCommand;
```

**`Commands/ResetPassword/ResetPasswordCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Identity.Application.Commands.ResetPassword;

public class ResetPasswordCommandHandler(
    IUserRepository _users,
    IPasswordHasher _hasher,
    IUnitOfWork _uow
) : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(command.Email);
        if (!emailResult.IsSuccess) return Result.Fail(IdentityErrors.InvalidCredentials);

        var user = await _users.GetByEmailAsync(emailResult.GetDataOrThrow(), cancellationToken);
        if (user is null) return Result.Fail(IdentityErrors.InvalidCredentials);

        if (!user.IsPasswordResetTokenValid(command.Token))
            return Result.Fail(IdentityErrors.TokenInvalid);

        var pwValidation = PasswordHash.ValidateRawPassword(command.NewPassword);
        if (!pwValidation.IsSuccess) return pwValidation;

        var hash       = _hasher.Hash(command.NewPassword);
        var hashResult = PasswordHash.FromHash(hash);
        if (!hashResult.IsSuccess) return Result.Fail(hashResult.GetErrorOrThrow());

        user.ChangePassword(hashResult.GetDataOrThrow());
        user.ClearPasswordResetToken();
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
```

**`Commands/ResetPassword/ResetPasswordCommandValidator.cs`**
```csharp
using FluentValidation;

namespace ECommerce.Identity.Application.Commands.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
```

---

**`Commands/ChangePassword/ChangePasswordCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Identity.Application.Commands.ChangePassword;

public record ChangePasswordCommand(Guid UserId, string OldPassword, string NewPassword)
    : IRequest<Result>, ITransactionalCommand;
```

**`Commands/ChangePassword/ChangePasswordCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Identity.Application.Commands.ChangePassword;

public class ChangePasswordCommandHandler(
    IUserRepository _users,
    IPasswordHasher _hasher,
    IUnitOfWork _uow
) : IRequestHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null) return Result.Fail(IdentityApplicationErrors.UserNotFound);

        if (!_hasher.Verify(command.OldPassword, user.PasswordHash.Hash))
            return Result.Fail(IdentityErrors.InvalidCredentials);

        var pwValidation = PasswordHash.ValidateRawPassword(command.NewPassword);
        if (!pwValidation.IsSuccess) return pwValidation;

        var hash       = _hasher.Hash(command.NewPassword);
        var hashResult = PasswordHash.FromHash(hash);
        if (!hashResult.IsSuccess) return Result.Fail(hashResult.GetErrorOrThrow());

        user.ChangePassword(hashResult.GetDataOrThrow());
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
```

**`Commands/ChangePassword/ChangePasswordCommandValidator.cs`**
```csharp
using FluentValidation;

namespace ECommerce.Identity.Application.Commands.ChangePassword;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.OldPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
```

---

**`Commands/UpdateProfile/UpdateProfileCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Identity.Application.DTOs;

namespace ECommerce.Identity.Application.Commands.UpdateProfile;

public record UpdateProfileCommand(Guid UserId, string FirstName, string LastName, string? PhoneNumber)
    : IRequest<Result<UserProfileDto>>, ITransactionalCommand;
```

**`Commands/UpdateProfile/UpdateProfileCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Application.Extensions;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Identity.Application.Commands.UpdateProfile;

public class UpdateProfileCommandHandler(
    IUserRepository _users,
    IUnitOfWork _uow
) : IRequestHandler<UpdateProfileCommand, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null) return Result<UserProfileDto>.Fail(IdentityApplicationErrors.UserNotFound);

        var nameResult = PersonName.Create(command.FirstName, command.LastName);
        if (!nameResult.IsSuccess) return Result<UserProfileDto>.Fail(nameResult.GetErrorOrThrow());

        user.UpdateProfile(nameResult.GetDataOrThrow(), command.PhoneNumber);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result<UserProfileDto>.Ok(user.ToProfileDto());
    }
}
```

**`Commands/UpdateProfile/UpdateProfileCommandValidator.cs`**
```csharp
using FluentValidation;

namespace ECommerce.Identity.Application.Commands.UpdateProfile;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}
```

---

**`Commands/AddAddress/AddAddressCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Identity.Application.DTOs;

namespace ECommerce.Identity.Application.Commands.AddAddress;

public record AddAddressCommand(Guid UserId, string Street, string City, string Country, string? PostalCode)
    : IRequest<Result<UserProfileDto>>, ITransactionalCommand;
```

**`Commands/AddAddress/AddAddressCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Application.Extensions;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Identity.Application.Commands.AddAddress;

public class AddAddressCommandHandler(
    IUserRepository _users,
    IUnitOfWork _uow
) : IRequestHandler<AddAddressCommand, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(AddAddressCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null) return Result<UserProfileDto>.Fail(IdentityApplicationErrors.UserNotFound);

        var result = user.AddAddress(command.Street, command.City, command.Country, command.PostalCode);
        if (!result.IsSuccess) return Result<UserProfileDto>.Fail(result.GetErrorOrThrow());

        await _uow.SaveChangesAsync(cancellationToken);
        return Result<UserProfileDto>.Ok(user.ToProfileDto());
    }
}
```

**`Commands/AddAddress/AddAddressCommandValidator.cs`**
```csharp
using FluentValidation;

namespace ECommerce.Identity.Application.Commands.AddAddress;

public class AddAddressCommandValidator : AbstractValidator<AddAddressCommand>
{
    public AddAddressCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
    }
}
```

---

### 5. Create queries

**`Queries/GetCurrentUser/GetCurrentUserQuery.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Application.DTOs;

namespace ECommerce.Identity.Application.Queries.GetCurrentUser;

public record GetCurrentUserQuery(Guid UserId) : IRequest<Result<UserProfileDto>>;
```

**`Queries/GetCurrentUser/GetCurrentUserQueryHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Application.Extensions;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Interfaces;

namespace ECommerce.Identity.Application.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler(IUserRepository _users)
    : IRequestHandler<GetCurrentUserQuery, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null) return Result<UserProfileDto>.Fail(IdentityApplicationErrors.UserNotFound);
        return Result<UserProfileDto>.Ok(user.ToProfileDto());
    }
}
```

---

**`Commands/Logout/LogoutCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Identity.Application.Commands.Logout;

public record LogoutCommand(Guid UserId, string RefreshToken) : IRequest<Result>, ITransactionalCommand;
```

**`Commands/Logout/LogoutCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Identity.Application.Commands.Logout;

public class LogoutCommandHandler(IUserRepository _users, IUnitOfWork _uow)
    : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null) return Result.Fail(IdentityApplicationErrors.UserNotFound);

        var token = user.GetActiveRefreshToken(command.RefreshToken);
        token?.Revoke("Logged out");  // silently succeed if token not found — idempotent
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
```

**`Commands/Logout/LogoutCommandValidator.cs`**
```csharp
using FluentValidation;

namespace ECommerce.Identity.Application.Commands.Logout;

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator() => RuleFor(x => x.UserId).NotEmpty();
}
```

---

**`Commands/SetDefaultAddress/SetDefaultAddressCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Identity.Application.DTOs;

namespace ECommerce.Identity.Application.Commands.SetDefaultAddress;

public record SetDefaultAddressCommand(Guid UserId, Guid AddressId)
    : IRequest<Result<UserProfileDto>>, ITransactionalCommand;
```

**`Commands/SetDefaultAddress/SetDefaultAddressCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Application.Extensions;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Identity.Application.Commands.SetDefaultAddress;

public class SetDefaultAddressCommandHandler(IUserRepository _users, IUnitOfWork _uow)
    : IRequestHandler<SetDefaultAddressCommand, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(SetDefaultAddressCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null) return Result<UserProfileDto>.Fail(IdentityApplicationErrors.UserNotFound);

        var result = user.SetDefaultShippingAddress(command.AddressId);
        if (!result.IsSuccess) return Result<UserProfileDto>.Fail(result.GetErrorOrThrow());

        await _uow.SaveChangesAsync(cancellationToken);
        return Result<UserProfileDto>.Ok(user.ToProfileDto());
    }
}
```

**`Commands/SetDefaultAddress/SetDefaultAddressCommandValidator.cs`**
```csharp
using FluentValidation;

namespace ECommerce.Identity.Application.Commands.SetDefaultAddress;

public class SetDefaultAddressCommandValidator : AbstractValidator<SetDefaultAddressCommand>
{
    public SetDefaultAddressCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AddressId).NotEmpty();
    }
}
```

---

**`Commands/DeleteAccount/DeleteAccountCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Identity.Application.Commands.DeleteAccount;

public record DeleteAccountCommand(Guid UserId) : IRequest<Result>, ITransactionalCommand;
```

**`Commands/DeleteAccount/DeleteAccountCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Identity.Application.Commands.DeleteAccount;

public class DeleteAccountCommandHandler(IUserRepository _users, IUnitOfWork _uow)
    : IRequestHandler<DeleteAccountCommand, Result>
{
    public async Task<Result> Handle(DeleteAccountCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null) return Result.Fail(IdentityApplicationErrors.UserNotFound);

        user.RevokeAllRefreshTokens("Account deleted");
        await _users.DeleteAsync(user, cancellationToken);  // add DeleteAsync to IUserRepository
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
```

> After writing this handler, add `Task DeleteAsync(User user, CancellationToken ct = default)` to `IUserRepository`.

**`Commands/DeleteAccount/DeleteAccountCommandValidator.cs`**
```csharp
using FluentValidation;

namespace ECommerce.Identity.Application.Commands.DeleteAccount;

public class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountCommandValidator() => RuleFor(x => x.UserId).NotEmpty();
}
```

---

**`Queries/GetUserById/GetUserByIdQuery.cs`** (admin-only)
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Application.DTOs;

namespace ECommerce.Identity.Application.Queries.GetUserById;

public record GetUserByIdQuery(Guid UserId) : IRequest<Result<UserProfileDto>>;
```

**`Queries/GetUserById/GetUserByIdQueryHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Application.Extensions;
using ECommerce.Identity.Domain.Interfaces;

namespace ECommerce.Identity.Application.Queries.GetUserById;

public class GetUserByIdQueryHandler(IUserRepository _users)
    : IRequestHandler<GetUserByIdQuery, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null) return Result<UserProfileDto>.Fail(IdentityApplicationErrors.UserNotFound);
        return Result<UserProfileDto>.Ok(user.ToProfileDto());
    }
}
```

---

### 5b. Create event handler — welcome email

The existing `AuthService.RegisterAsync` sends a welcome email (fire-and-forget). After migration, this becomes a domain event handler that reacts to `UserRegisteredEvent`. It lives in Application because sending email is not a domain concern.

**File: `Identity/ECommerce.Identity.Application/EventHandlers/SendWelcomeEmailOnUserRegisteredHandler.cs`**

```csharp
using MediatR;
using ECommerce.Identity.Domain.Events;
using Microsoft.Extensions.Logging;

namespace ECommerce.Identity.Application.EventHandlers;

// IEmailService is the existing interface from ECommerce.Application.Interfaces
// (or inject whatever email abstraction the project uses)
public class SendWelcomeEmailOnUserRegisteredHandler(
    IEmailService _email,  // existing interface — inject from ECommerce.Application
    ILogger<SendWelcomeEmailOnUserRegisteredHandler> _logger
) : INotificationHandler<UserRegisteredEvent>
{
    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _email.SendWelcomeEmailAsync(notification.Email, notification.UserId.ToString());
        }
        catch (Exception ex)
        {
            // Log and swallow — email failure must not crash registration (Rule 17)
            _logger.LogWarning(ex, "Failed to send welcome email to {Email}", notification.Email);
        }
    }
}
```

> `IEmailService` is already defined in `ECommerce.Application.Interfaces`. Add a reference from `Identity.Application` to `ECommerce.Application` or extract `IEmailService` to `SharedKernel`. Check what the existing project uses before wiring this up.

---

### 6. Create mapping extensions

**File: `Identity/ECommerce.Identity.Application/Extensions/UserMappingExtensions.cs`**

```csharp
using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Domain.Aggregates.User;

namespace ECommerce.Identity.Application.Extensions;

public static class UserMappingExtensions
{
    public static UserProfileDto ToProfileDto(this User user) =>
        new(
            Id:              user.Id,
            Email:           user.Email.Value,
            FirstName:       user.Name.First,
            LastName:        user.Name.Last,
            PhoneNumber:     user.PhoneNumber,
            Role:            user.Role.ToString(),
            IsEmailVerified: user.IsEmailVerified,
            Addresses:       user.Addresses.Select(a => new AddressDto(
                a.Id, a.Street, a.City, a.Country, a.PostalCode,
                a.IsDefaultShipping, a.IsDefaultBilling)).ToList()
        );
}
```

### 7. Register in Program.cs

In `src/backend/ECommerce.API/Program.cs`, inside the `AddMediatR` call, add:

```csharp
cfg.RegisterServicesFromAssembly(typeof(RegisterCommand).Assembly);
```

Also add the using:
```csharp
using ECommerce.Identity.Application.Commands.Register;
```

And register validators:
```csharp
builder.Services.AddValidatorsFromAssembly(typeof(RegisterCommand).Assembly);
```

### 8. Verify

```bash
cd src/backend
dotnet build Identity/ECommerce.Identity.Application/ECommerce.Identity.Application.csproj
dotnet build
```

---

## Acceptance Criteria

- [ ] `ECommerce.Identity.Application` project created and added to solution
- [ ] Dependencies: SharedKernel, Identity.Domain, MediatR, FluentValidation only
- [ ] `IPasswordHasher` and `IJwtTokenService` interfaces in Application (not Domain)
- [ ] `IdentityApplicationErrors.cs` created in Application (UserNotFound, EmailTaken, AddressNotFound)
- [ ] Commands: Register, Login, RefreshToken, VerifyEmail, ForgotPassword, ResetPassword, ChangePassword, UpdateProfile, AddAddress, **Logout**, **SetDefaultAddress**, **DeleteAccount** — each with handler + validator
- [ ] Queries: GetCurrentUser, **GetUserById** (admin) — each with handler
- [ ] DTOs: AuthTokenDto, UserProfileDto, AddressDto
- [ ] `UserMappingExtensions.ToProfileDto()` helper
- [ ] `SendWelcomeEmailOnUserRegisteredHandler` event handler (swallows exceptions)
- [ ] `IUserRepository.GetByRefreshTokenAsync` and `DeleteAsync` added to Domain interface
- [ ] Assembly registered in Program.cs `AddMediatR`
- [ ] Validators assembly registered in Program.cs
- [ ] `dotnet build` passes
