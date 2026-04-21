using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Events;
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

    private readonly List<Address> _addresses = new();
    private readonly List<RefreshToken> _refreshTokens = new();

    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User() { } // EF Core

    // ── Factory ─────────────────────────────────────────────────────────────────

    public static Result<User> Register(string rawEmail, string firstName, string lastName, PasswordHash passwordHash)
    {
        var emailResult = Email.Create(rawEmail);
        if (!emailResult.IsSuccess) return Result<User>.Fail(emailResult.GetErrorOrThrow());

        var nameResult = PersonName.Create(firstName, lastName);
        if (!nameResult.IsSuccess) return Result<User>.Fail(nameResult.GetErrorOrThrow());

        var user = new User
        {
            Email = emailResult.GetDataOrThrow(),
            Name = nameResult.GetDataOrThrow(),
            PasswordHash = passwordHash,
            Role = UserRole.Customer,
            IsEmailVerified = false,
            EmailVerificationToken = Guid.NewGuid().ToString("N"),
        };

        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Email.Value));
        return Result<User>.Ok(user);
    }

    internal static User Reconstitute(
        Guid id,
        Email email,
        PersonName name,
        PasswordHash passwordHash,
        string? phoneNumber,
        UserRole role,
        bool isEmailVerified,
        string? emailVerificationToken,
        string? passwordResetToken,
        DateTime? passwordResetExpiry,
        IEnumerable<Address> addresses,
        IEnumerable<RefreshToken> refreshTokens)
    {
        var user = new User
        {
            Id = id,
            Email = email,
            Name = name,
            PasswordHash = passwordHash,
            PhoneNumber = phoneNumber,
            Role = role,
            IsEmailVerified = isEmailVerified,
            EmailVerificationToken = emailVerificationToken,
            PasswordResetToken = passwordResetToken,
            PasswordResetExpiry = passwordResetExpiry,
        };
        user._addresses.AddRange(addresses);
        user._refreshTokens.AddRange(refreshTokens);
        return user;
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

    public void UpdateProfile(PersonName name, string? phoneNumber)
    {
        Name = name;
        PhoneNumber = phoneNumber;
    }

    // ── Password ────────────────────────────────────────────────────────────────

    public void ChangePassword(PasswordHash newHash)
    {
        PasswordHash = newHash;
        foreach (var token in _refreshTokens) token.Revoke("Password changed");
        AddDomainEvent(new PasswordChangedEvent(Id));
    }

    public void SetPasswordResetToken(string token, DateTime expiry)
    {
        PasswordResetToken = token;
        PasswordResetExpiry = expiry;
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
        return Result.Ok();
    }

    public Result SetDefaultShippingAddress(Guid addressId)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (address is null) return Result.Fail(IdentityErrors.AddressNotFound);

        foreach (var a in _addresses) a.SetDefaultShipping(false);
        address.SetDefaultShipping(true);
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
        var address = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (address is null) return Result.Fail(IdentityErrors.AddressNotFound);

        _addresses.Remove(address);
        return Result.Ok();
    }

    // ── Refresh Token Management ────────────────────────────────────────────────

    private const int MaxRefreshTokens = 5;

    public RefreshToken AddRefreshToken(string token, DateTime expiresAt)
    {
        var active = _refreshTokens.Where(t => t.IsActive).ToList();

        if (active.Count >= MaxRefreshTokens)
            active.MinBy(t => t.CreatedAt)?.Revoke("Max tokens reached");

        var refreshToken = new RefreshToken(Guid.NewGuid(), Id, token, expiresAt);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    public RefreshToken? GetActiveRefreshToken(string token) =>
        _refreshTokens.FirstOrDefault(t => t.Token == token && t.IsActive);

    public Result RevokeRefreshToken(string token)
    {
        var refreshToken = _refreshTokens.FirstOrDefault(t => t.Token == token);
        if (refreshToken is null) return Result.Fail(IdentityErrors.TokenInvalid);
        refreshToken.Revoke("Rotated");
        return Result.Ok();
    }

    public void RevokeAllRefreshTokens(string reason)
    {
        foreach (var t in _refreshTokens) t.Revoke(reason);
    }
}
