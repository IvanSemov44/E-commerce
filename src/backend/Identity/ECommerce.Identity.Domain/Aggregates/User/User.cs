using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Events;
using ECommerce.Identity.Domain.ValueObjects;

namespace ECommerce.Identity.Domain.Aggregates.User;

public enum UserRole { Customer, Admin }

public sealed class User : AggregateRoot
{
    public Email         Email        { get; private set; } = null!;
    public PersonName    Name         { get; private set; } = null!;
    public PasswordHash  PasswordHash { get; private set; } = null!;
    public string?       PhoneNumber  { get; private set; }
    public UserRole      Role         { get; private set; }
    public bool          IsEmailVerified       { get; private set; }
    public string?       EmailVerificationToken { get; private set; }
    public string?       PasswordResetToken    { get; private set; }
    public DateTime?     PasswordResetExpiry   { get; private set; }

    private readonly List<Address>      _addresses     = new();
    private readonly List<RefreshToken> _refreshTokens = new();

    public IReadOnlyCollection<Address>      Addresses     => _addresses.AsReadOnly();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User() { } // EF Core

    // Takes pre-validated value objects. Caller (RegisterCommandHandler) validates first.
    public static User Register(Email email, PersonName name, PasswordHash passwordHash)
    {
        var user = new User
        {
            Email                  = email,
            Name                   = name,
            PasswordHash           = passwordHash,
            Role                   = UserRole.Customer,
            IsEmailVerified        = false,
            EmailVerificationToken = Guid.NewGuid().ToString("N"),
        };
        user.AddDomainEvent(new UserRegisteredEvent(user.Id, email.Value));
        return user;
    }

    public Result VerifyEmail(string token)
    {
        if (IsEmailVerified)
            return Result.Fail(IdentityErrors.EmailAlreadyVerified);
        if (EmailVerificationToken != token)
            return Result.Fail(IdentityErrors.EmailTokenInvalid);

        IsEmailVerified        = true;
        EmailVerificationToken = null;
        AddDomainEvent(new EmailVerifiedEvent(Id));
        return Result.Ok();
    }

    public void UpdateProfile(PersonName name, string? phoneNumber)
    {
        Name        = name;
        PhoneNumber = phoneNumber;
    }

    public void ChangePassword(PasswordHash newHash)
    {
        PasswordHash = newHash;
        foreach (var t in _refreshTokens) t.Revoke("Password changed");
        AddDomainEvent(new PasswordChangedEvent(Id));
    }

    public void SetPasswordResetToken(string token, DateTime expiry)
    {
        PasswordResetToken  = token;
        PasswordResetExpiry = expiry;
    }

    public bool IsPasswordResetTokenValid(string token)
        => PasswordResetToken == token
        && PasswordResetExpiry.HasValue
        && DateTime.UtcNow < PasswordResetExpiry.Value;

    public void ClearPasswordResetToken()
    {
        PasswordResetToken  = null;
        PasswordResetExpiry = null;
    }

    public Result AddAddress(string street, string city, string country, string? postalCode)
    {
        if (string.IsNullOrWhiteSpace(street))  return Result.Fail(IdentityErrors.AddressStreetEmpty);
        if (string.IsNullOrWhiteSpace(city))    return Result.Fail(IdentityErrors.AddressCityEmpty);
        if (string.IsNullOrWhiteSpace(country)) return Result.Fail(IdentityErrors.AddressCountryEmpty);
        if (_addresses.Count >= 5)              return Result.Fail(IdentityErrors.AddressLimit);

        var address = new Address(Guid.NewGuid(), street.Trim(), city.Trim(), country.Trim(), postalCode?.Trim());

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

    public RefreshToken AddRefreshToken(string token, DateTime expiresAt)
    {
        var active = _refreshTokens.Where(t => !t.IsRevoked).ToList();
        if (active.Count >= 5)
            active.OrderBy(t => t.CreatedAt).First().Revoke("Max tokens reached");

        var refreshToken = new RefreshToken(Guid.NewGuid(), Id, token, expiresAt);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    public RefreshToken? GetActiveRefreshToken(string token)
        => _refreshTokens.FirstOrDefault(t => t.Token == token && t.IsActive);

    public void RevokeAllRefreshTokens(string reason)
    {
        foreach (var t in _refreshTokens) t.Revoke(reason);
    }
}
