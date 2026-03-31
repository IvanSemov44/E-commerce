using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Events;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Identity.Domain.Aggregates.User;

public enum UserRole { Customer, Admin }

/// <summary>
/// User aggregate root — manages identity, authentication, and profile data.
/// All child entities (Address, RefreshToken) are created/managed exclusively through this root.
/// </summary>
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

    /// <summary>
    /// Creates a new user from raw values. The aggregate is the sole guardian
    /// of invariants — it creates and validates value objects internally.
    /// Caller (RegisterCommandHandler) is responsible for uniqueness checks.
    /// </summary>
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
        if (string.IsNullOrWhiteSpace(street))  return Result.Fail(IdentityErrors.AddressStreetEmpty);
        if (string.IsNullOrWhiteSpace(city))    return Result.Fail(IdentityErrors.AddressCityEmpty);
        if (string.IsNullOrWhiteSpace(country)) return Result.Fail(IdentityErrors.AddressCountryEmpty);
        if (_addresses.Count >= MaxAddresses)   return Result.Fail(IdentityErrors.AddressLimit);

        var address = new Address(
            Guid.NewGuid(),
            street.Trim(), city.Trim(), country.Trim(), postalCode?.Trim());

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

    /// <summary>
    /// Revokes a specific refresh token by its value.
    /// Returns true if the token was found and revoked, false otherwise.
    /// </summary>
    public bool RevokeRefreshToken(string token)
    {
        var refreshToken = _refreshTokens.FirstOrDefault(t => t.Token == token);
        if (refreshToken is null) return false;
        refreshToken.Revoke("Rotated");
        return true;
    }

    public void RevokeAllRefreshTokens(string reason)
    {
        foreach (var t in _refreshTokens) t.Revoke(reason);
    }
}
