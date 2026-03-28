# Phase 2, Step 1: Identity Domain Project

**Prerequisite**: Phase 1 (Catalog) is complete and all tests pass.

---

## Context

We are migrating the Identity bounded context (users, authentication, authorization) from `AuthService`/`UserService` to DDD/CQRS. This step creates the Domain project only — no Application or Infrastructure yet.

**New concepts introduced in this phase:**
- `PersonName` is a multi-property value object (First + Last). It uses `class : ValueObject` not `record`, because equality is computed across two fields.
- `PasswordHash` holds a bcrypt hash — never plain text. The domain enforces password RULES (length, complexity). The Application layer calls `IPasswordHasher` to produce the actual hash.
- `Address` is a child entity (NOT a value object) because a user can have multiple addresses and you can say "delete THIS address" — identity matters.
- `RefreshToken` is also a child entity managed exclusively through the User aggregate.

---

## Task: Create ECommerce.Identity.Domain Project

### 1. Create the project

```bash
cd src/backend
mkdir -p Identity
dotnet new classlib -n ECommerce.Identity.Domain -f net10.0 -o Identity/ECommerce.Identity.Domain
dotnet sln ../../ECommerce.sln add Identity/ECommerce.Identity.Domain/ECommerce.Identity.Domain.csproj

# Only dependency: SharedKernel
dotnet add Identity/ECommerce.Identity.Domain/ECommerce.Identity.Domain.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj

# Delete auto-generated file
rm Identity/ECommerce.Identity.Domain/Class1.cs
```

### 2. Create domain errors

**File: `Identity/ECommerce.Identity.Domain/Errors/IdentityErrors.cs`**

```csharp
using ECommerce.SharedKernel.Results;

namespace ECommerce.Identity.Domain.Errors;

public static class IdentityErrors
{
    // Email
    public static readonly DomainError EmailEmpty     = new("EMAIL_EMPTY",      "Email is required.");
    public static readonly DomainError EmailTooLong   = new("EMAIL_TOO_LONG",   "Email must not exceed 256 characters.");
    public static readonly DomainError EmailInvalid   = new("EMAIL_INVALID",    "Email format is invalid.");
    public static readonly DomainError EmailAlreadyVerified = new("EMAIL_ALREADY_VERIFIED", "Email is already verified.");
    public static readonly DomainError EmailTokenInvalid    = new("EMAIL_TOKEN_INVALID",    "Email verification token is invalid.");

    // PersonName
    public static readonly DomainError NameFirstEmpty = new("NAME_FIRST_EMPTY", "First name is required.");
    public static readonly DomainError NameLastEmpty  = new("NAME_LAST_EMPTY",  "Last name is required.");
    public static readonly DomainError NameTooLong    = new("NAME_TOO_LONG",    "First and last name must not exceed 100 characters each.");

    // Password
    public static readonly DomainError PasswordEmpty    = new("PASSWORD_EMPTY",    "Password is required.");
    public static readonly DomainError PasswordTooShort = new("PASSWORD_TOO_SHORT","Password must be at least 8 characters.");
    public static readonly DomainError PasswordNoUpper  = new("PASSWORD_NO_UPPER", "Password must contain at least one uppercase letter.");
    public static readonly DomainError PasswordNoDigit  = new("PASSWORD_NO_DIGIT", "Password must contain at least one digit.");
    public static readonly DomainError PasswordHashEmpty = new("PASSWORD_HASH_EMPTY", "Password hash cannot be empty.");

    // Auth (aggregate-level — detectable without a repo lookup)
    public static readonly DomainError InvalidCredentials = new("INVALID_CREDENTIALS", "Invalid email or password.");
    public static readonly DomainError TokenInvalid       = new("TOKEN_INVALID",        "Refresh token is invalid or expired.");
    public static readonly DomainError TokenRevoked       = new("TOKEN_REVOKED",        "Refresh token has been revoked.");

    // NOTE: UserNotFound and EmailTaken are NOT here — they require a repository lookup,
    // making them application-layer concerns. They live in IdentityApplicationErrors (step-2).

    // Address (raised by User aggregate — no repo lookup needed)
    public static readonly DomainError AddressLimit        = new("ADDRESS_LIMIT",         "A user cannot have more than 5 addresses.");
    public static readonly DomainError AddressNotFound     = new("ADDRESS_NOT_FOUND",     "Address not found.");
    public static readonly DomainError AddressStreetEmpty  = new("ADDRESS_STREET_EMPTY",  "Street is required.");
    public static readonly DomainError AddressCityEmpty    = new("ADDRESS_CITY_EMPTY",    "City is required.");
    public static readonly DomainError AddressCountryEmpty = new("ADDRESS_COUNTRY_EMPTY", "Country is required.");
}
```

### 3. Create value objects

**File: `Identity/ECommerce.Identity.Domain/ValueObjects/Email.cs`**

```csharp
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Domain.Errors;

namespace ECommerce.Identity.Domain.ValueObjects;

// Single-property VO → use sealed record (Rule 8: sealed prevents subclassing that breaks value equality)
public sealed record Email
{
    public string Value { get; }
    private Email(string value) => Value = value;

    public static Result<Email> Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result<Email>.Fail(IdentityErrors.EmailEmpty);

        var normalized = raw.Trim().ToLowerInvariant();

        if (normalized.Length > 256)
            return Result<Email>.Fail(IdentityErrors.EmailTooLong);

        if (!normalized.Contains('@') || normalized.IndexOf('.', normalized.IndexOf('@')) < 0)
            return Result<Email>.Fail(IdentityErrors.EmailInvalid);

        return Result<Email>.Ok(new Email(normalized));
    }
}
```

**File: `Identity/ECommerce.Identity.Domain/ValueObjects/PersonName.cs`**

```csharp
using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Domain.Errors;

namespace ECommerce.Identity.Domain.ValueObjects;

// Multi-property VO → use sealed class : ValueObject (equality across two fields)
public sealed class PersonName : ValueObject
{
    public string First { get; private set; } = null!;
    public string Last  { get; private set; } = null!;
    public string FullName => $"{First} {Last}";

    private PersonName() { } // EF Core
    private PersonName(string first, string last) { First = first; Last = last; }

    public static Result<PersonName> Create(string first, string last)
    {
        if (string.IsNullOrWhiteSpace(first))
            return Result<PersonName>.Fail(IdentityErrors.NameFirstEmpty);
        if (string.IsNullOrWhiteSpace(last))
            return Result<PersonName>.Fail(IdentityErrors.NameLastEmpty);
        if (first.Trim().Length > 100 || last.Trim().Length > 100)
            return Result<PersonName>.Fail(IdentityErrors.NameTooLong);

        return Result<PersonName>.Ok(new PersonName(first.Trim(), last.Trim()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return First.ToLowerInvariant();
        yield return Last.ToLowerInvariant();
    }
}
```

**File: `Identity/ECommerce.Identity.Domain/ValueObjects/PasswordHash.cs`**

```csharp
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Domain.Errors;

namespace ECommerce.Identity.Domain.ValueObjects;

// The domain holds a hash, NEVER plain text.
// Infrastructure calls IPasswordHasher to produce the hash; domain wraps it here.
public sealed record PasswordHash
{
    public string Hash { get; }
    private PasswordHash(string hash) => Hash = hash;

    // Called by Infrastructure after hashing
    public static Result<PasswordHash> FromHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return Result<PasswordHash>.Fail(IdentityErrors.PasswordHashEmpty);
        return Result<PasswordHash>.Ok(new PasswordHash(hash));
    }

    // Domain enforces password POLICY before Infrastructure hashes it
    public static Result ValidateRawPassword(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result.Fail(IdentityErrors.PasswordEmpty);
        if (raw.Length < 8)
            return Result.Fail(IdentityErrors.PasswordTooShort);
        if (!raw.Any(char.IsUpper))
            return Result.Fail(IdentityErrors.PasswordNoUpper);
        if (!raw.Any(char.IsDigit))
            return Result.Fail(IdentityErrors.PasswordNoDigit);
        return Result.Ok();
    }
}
```

### 4. Create AssemblyInfo for InternalsVisibleTo

EF Core needs to access `internal` child entities from the Infrastructure project to configure mappings. Grant this at the assembly level (Rule 5).

**File: `Identity/ECommerce.Identity.Domain/Properties/AssemblyInfo.cs`**

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ECommerce.Identity.Infrastructure")]
```

### 5. Create child entities

**File: `Identity/ECommerce.Identity.Domain/Aggregates/User/Address.cs`**

```csharp
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Identity.Domain.Aggregates.User;

// Child entity — NOT a value object. A user has many addresses.
// Each address has identity: you can say "delete THIS address."
// internal sealed: enforces aggregate boundary at compile time (Rule 5).
// Only User.AddAddress() can create addresses.
internal sealed class Address : Entity
{
    public string  Street    { get; private set; } = null!;
    public string  City      { get; private set; } = null!;
    public string  Country   { get; private set; } = null!;
    public string? PostalCode { get; private set; }
    public bool IsDefaultShipping { get; private set; }
    public bool IsDefaultBilling  { get; private set; }

    private Address() { } // EF Core

    internal Address(Guid id, string street, string city, string country, string? postalCode)
    {
        Id          = id;
        Street      = street;
        City        = city;
        Country     = country;
        PostalCode  = postalCode;
    }

    internal void SetDefaultShipping(bool value) => IsDefaultShipping = value;
    internal void SetDefaultBilling(bool value)  => IsDefaultBilling  = value;
}
```

**File: `Identity/ECommerce.Identity.Domain/Aggregates/User/RefreshToken.cs`**

```csharp
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Identity.Domain.Aggregates.User;

// internal sealed: enforces aggregate boundary at compile time (Rule 5).
// Only User.AddRefreshToken() can create refresh tokens.
internal sealed class RefreshToken : Entity
{
    public Guid     UserId    { get; private set; }
    public string   Token     { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool     IsRevoked { get; private set; }
    public string?  RevokedReason { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsActive  => !IsRevoked && !IsExpired;

    private RefreshToken() { } // EF Core

    internal RefreshToken(Guid id, Guid userId, string token, DateTime expiresAt)
    {
        Id        = id;
        UserId    = userId;
        Token     = token;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    internal void Revoke(string reason)
    {
        IsRevoked     = true;
        RevokedReason = reason;
    }
}
```

### 6. Create domain events

**File: `Identity/ECommerce.Identity.Domain/Events/UserRegisteredEvent.cs`**
```csharp
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Identity.Domain.Events;

public record UserRegisteredEvent(Guid UserId, string Email) : DomainEventBase;
```

**File: `Identity/ECommerce.Identity.Domain/Events/EmailVerifiedEvent.cs`**
```csharp
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Identity.Domain.Events;

public record EmailVerifiedEvent(Guid UserId) : DomainEventBase;
```

**File: `Identity/ECommerce.Identity.Domain/Events/PasswordChangedEvent.cs`**
```csharp
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Identity.Domain.Events;

public record PasswordChangedEvent(Guid UserId) : DomainEventBase;
```

### 7. Create User aggregate

**File: `Identity/ECommerce.Identity.Domain/Aggregates/User/User.cs`**

```csharp
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
```

### 8. Create repository interface

**File: `Identity/ECommerce.Identity.Domain/Interfaces/IUserRepository.cs`**

```csharp
using ECommerce.Identity.Domain.Aggregates.User;
using ECommerce.Identity.Domain.ValueObjects;

namespace ECommerce.Identity.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?>  GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?>  GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<bool>   EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<User?>  GetByRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(User user, CancellationToken cancellationToken = default);
}
```

### 9. Verify

```bash
cd src/backend
dotnet build Identity/ECommerce.Identity.Domain/ECommerce.Identity.Domain.csproj
dotnet build  # Entire solution still builds
```

---

## Acceptance Criteria

- [ ] `ECommerce.Identity.Domain` project created and added to solution
- [ ] Only dependency: `ECommerce.SharedKernel`
- [ ] No `using Microsoft.EntityFrameworkCore` anywhere in Domain
- [ ] `Email` (`sealed record`), `PersonName` (`sealed class : ValueObject`), `PasswordHash` (`sealed record`) — all created with `Result<T>` factory methods and sealed
- [ ] `User` aggregate is `sealed class`; has `Register`, `VerifyEmail`, `ChangePassword`, `UpdateProfile`, `AddAddress`, `SetDefaultShippingAddress`, `DeleteAddress`, `AddRefreshToken`, `GetActiveRefreshToken` methods
- [ ] `Address` and `RefreshToken` are `internal sealed class` — only User can create them
- [ ] `Properties/AssemblyInfo.cs` with `InternalsVisibleTo("ECommerce.Identity.Infrastructure")` so EF Core can configure internal entities
- [ ] `UserRole` enum defined
- [ ] 3 domain events defined as records
- [ ] `IUserRepository` interface has all 6 methods including `GetByRefreshTokenAsync` and `DeleteAsync`
- [ ] `IdentityErrors` has NO `EmailTaken` or `UserNotFound` — those live in `IdentityApplicationErrors` (step-2)
- [ ] `dotnet build` passes for Domain project and entire solution
