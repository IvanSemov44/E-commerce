using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Events;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Enums;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Identity.Domain.Aggregates.User;

public sealed class User : AggregateRoot
{
    // ── Properties ──────────────────────────────────────────────────────────────

    public Email Email { get; private set; } = null!;
    public PersonName Name { get; private set; } = null!;
    public PasswordHash PasswordHash { get; private set; } = null!;
    public string? PhoneNumber { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetExpiry { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public bool IsDeleted => DeletedAt.HasValue;

    private readonly List<Address> _addresses = new();
    private readonly List<RefreshToken> _refreshTokens = new();

    public IReadOnlyCollection<Address> Addresses => _addresses.Where(a => !a.IsDeleted).ToList().AsReadOnly();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.Where(t => !t.IsDeleted).ToList().AsReadOnly();

    private User() { } // EF Core

    // ── Factory ─────────────────────────────────────────────────────────────────

    public static Result<User> Register(string rawEmail, string firstName, string lastName, string rawPassword, IPasswordHasher hasher)
    {
        var emailResult = Email.Create(rawEmail);
        if (!emailResult.IsSuccess) return Result<User>.Fail(emailResult.GetErrorOrThrow());

        var nameResult = PersonName.Create(firstName, lastName);
        if (!nameResult.IsSuccess) return Result<User>.Fail(nameResult.GetErrorOrThrow());

        var pwValidation = PasswordHash.ValidateRawPassword(rawPassword);
        if (!pwValidation.IsSuccess) return Result<User>.Fail(pwValidation.GetErrorOrThrow());

        var hashResult = PasswordHash.FromHash(hasher.Hash(rawPassword));
        if (!hashResult.IsSuccess) return Result<User>.Fail(hashResult.GetErrorOrThrow());

        var user = new User
        {
            Email = emailResult.GetDataOrThrow(),
            Name = nameResult.GetDataOrThrow(),
            PasswordHash = hashResult.GetDataOrThrow(),
            Role = UserRole.Customer,
            IsEmailVerified = false,
            EmailVerificationToken = Guid.NewGuid().ToString("N"),
        };

        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Email.Value));
        return Result<User>.Ok(user);
    }

    // ── Email Verification ──────────────────────────────────────────────────────

    public Result VerifyEmail(string token)
    {
        if (IsEmailVerified)
            return Result.Fail(IdentityErrors.EmailAlreadyVerified);

        if (EmailVerificationToken != token)
            return Result.Fail(IdentityErrors.EmailTokenInvalid);

        IsEmailVerified = true;
        EmailVerificationToken = null;
        AddDomainEvent(new EmailVerifiedEvent(Id));
        return Result.Ok();
    }

    // ── Profile ─────────────────────────────────────────────────────────────────

    public Result UpdateProfile(string firstName, string lastName, string? phoneNumber)
    {
        var nameResult = PersonName.Create(firstName, lastName);
        if (!nameResult.IsSuccess) return Result.Fail(nameResult.GetErrorOrThrow());

        Name = nameResult.GetDataOrThrow();
        PhoneNumber = phoneNumber;
        return Result.Ok();
    }

    // ── Password ────────────────────────────────────────────────────────────────

    public bool VerifyPassword(string rawPassword, IPasswordHasher hasher)
        => hasher.Verify(rawPassword, PasswordHash.Hash);

    public Result ChangePassword(string oldPassword, string newPassword, IPasswordHasher hasher)
    {
        if (!hasher.Verify(oldPassword, PasswordHash.Hash))
            return Result.Fail(IdentityErrors.InvalidCredentials);

        var setResult = SetNewPassword(newPassword, hasher);
        if (!setResult.IsSuccess) return setResult;

        foreach (var token in _refreshTokens)
            token.Revoke("Password changed");
        AddDomainEvent(new PasswordChangedEvent(Id));
        return Result.Ok();
    }

    public Result ResetPassword(string newPassword, IPasswordHasher hasher)
    {
        var setResult = SetNewPassword(newPassword, hasher);
        if (!setResult.IsSuccess) return setResult;

        foreach (var token in _refreshTokens)
            token.Revoke("Password reset");
        ClearPasswordResetToken();
        AddDomainEvent(new PasswordChangedEvent(Id));
        return Result.Ok();
    }

    private Result SetNewPassword(string newPassword, IPasswordHasher hasher)
    {
        var pwValidation = PasswordHash.ValidateRawPassword(newPassword);
        if (!pwValidation.IsSuccess) return pwValidation;

        var hashResult = PasswordHash.FromHash(hasher.Hash(newPassword));
        if (!hashResult.IsSuccess) return Result.Fail(hashResult.GetErrorOrThrow());

        PasswordHash = hashResult.GetDataOrThrow();
        return Result.Ok();
    }

    public void RequestPasswordReset()
    {
        PasswordResetToken = Guid.NewGuid().ToString("N");
        PasswordResetExpiry = DateTime.UtcNow.AddHours(1);
    }

    public bool IsPasswordResetTokenValid(string token) =>
        PasswordResetToken == token
        && PasswordResetExpiry.HasValue
        && DateTime.UtcNow < PasswordResetExpiry.Value;

    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetExpiry = null;
    }

    // ── Address Management ──────────────────────────────────────────────────────

    private const int MaxAddresses = 5;

    public Result AddAddress(string street, string city, string country, string? postalCode)
    {
        if (_addresses.Count >= MaxAddresses)
            return Result.Fail(IdentityErrors.AddressLimit);

        var result = Address.Create(street, city, country, postalCode);
        if (!result.IsSuccess) return Result.Fail(result.GetErrorOrThrow());

        var address = result.GetDataOrThrow();
        if (_addresses.Count == 0)
        {
            address.SetDefaultShipping(true);
            address.SetDefaultBilling(true);
        }

        _addresses.Add(address);
        AddDomainEvent(new AddressAddedEvent(Id, address.Id, address.Street, address.City, address.Country, address.PostalCode));
        return Result.Ok();
    }

    public Result SetDefaultShippingAddress(Guid addressId)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (address is null) return Result.Fail(IdentityErrors.AddressNotFound);

        foreach (var a in _addresses) a.SetDefaultShipping(false);
        address.SetDefaultShipping(true);
        AddDomainEvent(new AddressDefaultShippingChangedEvent(Id, address.Id, address.Street, address.City, address.Country, address.PostalCode));
        return Result.Ok();
    }

    public Result SetDefaultBillingAddress(Guid addressId)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (address is null) return Result.Fail(IdentityErrors.AddressNotFound);

        foreach (var a in _addresses) a.SetDefaultBilling(false);
        address.SetDefaultBilling(true);
        return Result.Ok();
    }

    public Result DeleteAddress(Guid addressId)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId && !a.IsDeleted);
        if (address is null) return Result.Fail(IdentityErrors.AddressNotFound);

        AddDomainEvent(new AddressDeletedEvent(Id, address.Id, address.Street, address.City, address.Country, address.PostalCode));
        address.Delete();

        var activeAddresses = _addresses.Where(a => !a.IsDeleted).ToList();
        if (activeAddresses.Count > 0)
        {
            if (!activeAddresses.Any(a => a.IsDefaultShipping))
                activeAddresses[0].SetDefaultShipping(true);

            if (!activeAddresses.Any(a => a.IsDefaultBilling))
                activeAddresses[0].SetDefaultBilling(true);
        }

        return Result.Ok();
    }

    // ── Refresh Token Management ────────────────────────────────────────────────

    private const int MaxRefreshTokens = 5;

    public RefreshToken AddRefreshToken(string token, DateTime expiresAt)
    {
        var active = _refreshTokens.Where(t => t.IsActive && !t.IsDeleted).ToList();

        if (active.Count >= MaxRefreshTokens)
            active.MinBy(t => t.CreatedAt)?.Revoke("Max tokens reached");

        var refreshToken = new RefreshToken(Guid.NewGuid(), Id, token, expiresAt);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    public RefreshToken? GetActiveRefreshToken(string token) =>
        _refreshTokens.FirstOrDefault(t => t.Token == token && t.IsActive && !t.IsDeleted);

    public Result<string> RotateRefreshToken(string currentToken, string newRawToken, DateTime expiresAt)
    {
        var existing = GetActiveRefreshToken(currentToken);
        if (existing is null) return Result<string>.Fail(IdentityErrors.TokenRevoked);

        existing.Revoke("Rotated");
        AddRefreshToken(newRawToken, expiresAt);
        return Result<string>.Ok(newRawToken);
    }

    public Result RevokeRefreshToken(string token)
    {
        var refreshToken = _refreshTokens.FirstOrDefault(t => t.Token == token && !t.IsDeleted);

        if (refreshToken is null)
            return Result.Fail(IdentityErrors.TokenInvalid);

        refreshToken.Revoke("Logged out");
        return Result.Ok();
    }

    public void RevokeAllRefreshTokens(string reason)
    {
        foreach (var t in _refreshTokens.Where(t => !t.IsDeleted)) t.Revoke(reason);
    }

    public void DeleteAccount()
    {
        if (IsDeleted)
            return;

        DeletedAt = DateTime.UtcNow;

        foreach (var address in _addresses.Where(a => !a.IsDeleted))
            address.Delete();

        foreach (var token in _refreshTokens.Where(t => !t.IsDeleted))
            token.Delete("Account deleted");

        EmailVerificationToken = null;
        PasswordResetToken = null;
        PasswordResetExpiry = null;
    }
}
