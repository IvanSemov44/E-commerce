# Phase 2: Identity Bounded Context

**Prerequisite**: Phase 1 complete.

**Learn**: Deep value objects with complex validation, Password as a domain concept, multi-property VOs with custom equality, auth as an Application concern (not domain).

---

## What's New in This Phase

Phase 1 had simple value objects: single-property wrappers like `ProductName`, `Slug`, `Sku`. This phase introduces:

1. **Value objects with complex validation** — `Email` normalizes, `PersonName` has two parts, `PhoneNumber` strips and validates format
2. **Password as a domain concept** — the domain knows about *credentials*, but hashing is infrastructure. The VO holds the hash, never the plain text.
3. **Address as a child entity** — Address has identity (a user has many addresses, each distinct). It is NOT a value object despite looking like one.
4. **Auth is Application, not Domain** — JWT generation, token refresh, password comparison — none of this is domain logic. The domain enforces *that* a user has valid credentials; the Application decides *how* to authenticate.

---

## Old Service → New Handler Mapping

| Old Method | New Handler |
|-----------|-------------|
| `AuthService.RegisterAsync(dto)` | `RegisterCommand` |
| `AuthService.LoginAsync(dto)` | `LoginCommand` |
| `AuthService.RefreshTokenAsync(token)` | `RefreshTokenCommand` |
| `AuthService.ForgotPasswordAsync(email)` | `ForgotPasswordCommand` |
| `AuthService.ResetPasswordAsync(token, newPw)` | `ResetPasswordCommand` |
| `UserService.GetCurrentUserAsync()` | `GetCurrentUserQuery` |
| `UserService.GetUserByIdAsync(id)` | `GetUserByIdQuery` (admin) |
| `UserService.UpdateProfileAsync(dto)` | `UpdateProfileCommand` |
| `UserService.AddAddressAsync(dto)` | `AddAddressCommand` |
| `UserService.SetDefaultAddressAsync(id)` | `SetDefaultAddressCommand` |
| `UserService.DeleteAccountAsync()` | `DeleteAccountCommand` |

Write characterization tests against all of these first.

---

## Step 1: Domain Project

### Value Objects

**Email** — normalizes to lowercase, validates format:

```csharp
public record Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new IdentityDomainException("EMAIL_EMPTY", "Email cannot be empty.");

        var normalized = raw.Trim().ToLowerInvariant();

        if (normalized.Length > 256)
            throw new IdentityDomainException("EMAIL_TOO_LONG", "Email cannot exceed 256 characters.");

        // Basic structural check — real validation via MX lookup is infrastructure
        if (!normalized.Contains('@') || normalized.IndexOf('.', normalized.IndexOf('@')) < 0)
            throw new IdentityDomainException("EMAIL_INVALID", "Email format is not valid.");

        return new Email(normalized);
    }
}
```

**PersonName** — two properties, use `class : ValueObject` because equality is First + Last:

```csharp
public class PersonName : ValueObject
{
    public string First { get; }
    public string Last { get; }
    public string FullName => $"{First} {Last}";

    private PersonName() { }  // EF Core
    private PersonName(string first, string last) { First = first; Last = last; }

    public static PersonName Create(string first, string last)
    {
        if (string.IsNullOrWhiteSpace(first))
            throw new IdentityDomainException("NAME_FIRST_EMPTY", "First name cannot be empty.");
        if (string.IsNullOrWhiteSpace(last))
            throw new IdentityDomainException("NAME_LAST_EMPTY", "Last name cannot be empty.");
        if (first.Trim().Length > 100 || last.Trim().Length > 100)
            throw new IdentityDomainException("NAME_TOO_LONG", "Name part cannot exceed 100 characters.");

        return new PersonName(first.Trim(), last.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return First.ToLowerInvariant();
        yield return Last.ToLowerInvariant();
    }
}
```

**Password** — the domain holds a hash, never plain text. Hashing is Infrastructure:

```csharp
// The domain concept: a password hash that can be compared
public record PasswordHash
{
    public string Hash { get; }

    private PasswordHash(string hash) => Hash = hash;

    // Called by Infrastructure's IPasswordHasher after hashing
    public static PasswordHash FromHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new IdentityDomainException("PASSWORD_HASH_EMPTY", "Password hash cannot be empty.");
        return new PasswordHash(hash);
    }

    // Domain enforces the password policy BEFORE hashing
    public static void ValidateRawPassword(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new IdentityDomainException("PASSWORD_EMPTY", "Password cannot be empty.");
        if (raw.Length < 8)
            throw new IdentityDomainException("PASSWORD_TOO_SHORT", "Password must be at least 8 characters.");
        if (!raw.Any(char.IsUpper))
            throw new IdentityDomainException("PASSWORD_NO_UPPER", "Password must contain an uppercase letter.");
        if (!raw.Any(char.IsDigit))
            throw new IdentityDomainException("PASSWORD_NO_DIGIT", "Password must contain a digit.");
    }
}
```

**Why this split?** The domain knows the RULES for a valid password (length, complexity). Hashing (bcrypt, Argon2) is a cryptographic infrastructure concern. The handler:
1. Calls `PasswordHash.ValidateRawPassword(command.Password)` — domain enforces policy
2. Calls `IPasswordHasher.Hash(command.Password)` — infrastructure hashes it
3. Calls `PasswordHash.FromHash(hash)` — domain wraps the result

### Address — child entity, NOT a value object

```csharp
// Aggregates/User/Address.cs
public class Address : Entity
{
    public string Street { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string Country { get; private set; } = null!;
    public string? PostalCode { get; private set; }
    public bool IsDefaultShipping { get; private set; }
    public bool IsDefaultBilling { get; private set; }

    private Address() { }  // EF Core

    internal Address(Guid id, string street, string city, string country, string? postalCode)
    {
        Id = id;
        Street = street;
        City = city;
        Country = country;
        PostalCode = postalCode;
    }

    internal void SetDefaultShipping(bool value) => IsDefaultShipping = value;
    internal void SetDefaultBilling(bool value) => IsDefaultBilling = value;
}
```

**Why not a value object?** A user can have multiple addresses. Each address is distinct — you can say "delete THIS address" (identity matters). Value objects don't have identity. Addresses are child entities of the User aggregate.

### User aggregate

```csharp
// Aggregates/User/User.cs
public class User : AggregateRoot
{
    public Email Email { get; private set; } = null!;
    public PersonName Name { get; private set; } = null!;
    public PasswordHash PasswordHash { get; private set; } = null!;
    public string? PhoneNumber { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public string? EmailVerificationToken { get; private set; }

    private readonly List<Address> _addresses = new();
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();

    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User() { }

    public static User Register(Email email, PersonName name, PasswordHash passwordHash)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = name,
            PasswordHash = passwordHash,
            Role = UserRole.Customer,
            IsEmailVerified = false,
            EmailVerificationToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.AddDomainEvent(new UserRegisteredEvent(user.Id, email.Value));
        return user;
    }

    public void VerifyEmail(string token)
    {
        if (IsEmailVerified)
            throw new IdentityDomainException("EMAIL_ALREADY_VERIFIED", "Email is already verified.");
        if (EmailVerificationToken != token)
            throw new IdentityDomainException("EMAIL_TOKEN_INVALID", "Verification token is invalid.");

        IsEmailVerified = true;
        EmailVerificationToken = null;
        AddDomainEvent(new EmailVerifiedEvent(Id));
    }

    public void ChangePassword(PasswordHash newHash)
    {
        PasswordHash = newHash;
        // Revoke all existing refresh tokens when password changes
        foreach (var token in _refreshTokens)
            token.Revoke("Password changed");
    }

    public void AddAddress(string street, string city, string country, string? postalCode)
    {
        if (_addresses.Count >= 5)
            throw new IdentityDomainException("ADDRESS_LIMIT", "Cannot have more than 5 addresses.");

        var address = new Address(Guid.NewGuid(), street, city, country, postalCode);

        if (_addresses.Count == 0)
        {
            address.SetDefaultShipping(true);
            address.SetDefaultBilling(true);
        }

        _addresses.Add(address);
    }

    public void SetDefaultShippingAddress(Guid addressId)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId)
            ?? throw new IdentityDomainException("ADDRESS_NOT_FOUND", "Address not found.");

        foreach (var a in _addresses) a.SetDefaultShipping(false);
        address.SetDefaultShipping(true);
    }

    public RefreshToken AddRefreshToken(string token, DateTime expiresAt)
    {
        // Revoke old tokens beyond max count
        var activeTokens = _refreshTokens.Where(t => !t.IsRevoked).ToList();
        if (activeTokens.Count >= 5)
            activeTokens.OrderBy(t => t.CreatedAt).First().Revoke("Max tokens reached");

        var refreshToken = new RefreshToken(Guid.NewGuid(), Id, token, expiresAt);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }
}
```

### Auth as Application concern — IPasswordHasher and IJwtTokenService

These interfaces live in the **Application** project, not the domain. The domain doesn't know HOW to hash or generate tokens.

```csharp
// In Identity.Application/Interfaces/IPasswordHasher.cs
public interface IPasswordHasher
{
    string Hash(string rawPassword);
    bool Verify(string rawPassword, string hash);
}

// In Identity.Application/Interfaces/IJwtTokenService.cs
public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
```

Implementations live in `Identity.Infrastructure/Services/`.

### Login command — auth is orchestration

```csharp
// Commands/Login/LoginCommandHandler.cs
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthTokenDto>>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IUnitOfWork _uow;

    public async Task<Result<AuthTokenDto>> Handle(LoginCommand command, CancellationToken ct)
    {
        // 1. Find user by email
        var user = await _users.GetByEmailAsync(Email.Create(command.Email), ct);
        if (user is null)
            return Result<AuthTokenDto>.Fail(ErrorCodes.Identity.InvalidCredentials, "Invalid email or password.");

        // 2. Verify password — infrastructure concern
        if (!_hasher.Verify(command.Password, user.PasswordHash.Hash))
            return Result<AuthTokenDto>.Fail(ErrorCodes.Identity.InvalidCredentials, "Invalid email or password.");

        // 3. Generate tokens — infrastructure concern
        var accessToken = _jwt.GenerateAccessToken(user);
        var rawRefreshToken = _jwt.GenerateRefreshToken();

        // 4. Domain method records the refresh token
        var refreshToken = user.AddRefreshToken(rawRefreshToken, DateTime.UtcNow.AddDays(30));
        await _uow.SaveChangesAsync(ct);

        return Result<AuthTokenDto>.Ok(new AuthTokenDto(accessToken, rawRefreshToken, user.Id));
    }
}
```

Notice: `_hasher.Verify()` and `_jwt.GenerateAccessToken()` are infrastructure. The domain method `user.AddRefreshToken()` records the token. The domain doesn't know bcrypt exists.

---

## Key Decisions in This Phase

**Why same `InvalidCredentials` error for both "user not found" AND "wrong password"?** Security. Never reveal whether an email exists. Return the same error code for both cases so an attacker can't enumerate valid emails.

**Why is `EmailVerificationToken` a string not a VO?** It's a one-time-use random string with no domain behavior or validation rules. A VO would add ceremony with no value here.

**Why does `Address` have an `internal` constructor?** Only `User.AddAddress(...)` should create addresses. If the constructor were `public`, infrastructure code could bypass the aggregate root and create orphan addresses.

---

## Definition of Done

- [ ] Characterization tests written and passing against old AuthService + UserService
- [ ] `Email`, `PersonName`, `PasswordHash` value objects with validation
- [ ] `User` aggregate with `Register`, `VerifyEmail`, `ChangePassword`, `AddAddress`, `AddRefreshToken` domain methods
- [ ] `Address` and `RefreshToken` as child entities (not value objects)
- [ ] `IPasswordHasher` and `IJwtTokenService` interfaces in Application (not Domain)
- [ ] Implementations in Infrastructure
- [ ] `LoginCommand` handler orchestrates without domain knowing about hashing
- [ ] EF configurations: Email as `HasConversion`, PersonName as `OwnsOne`
- [ ] Old `AuthService`, `UserService` deleted after characterization tests pass on new handlers

## What You Learned in Phase 2

- Complex value objects: when to use `record` (single property) vs `class : ValueObject` (multiple properties, custom equality)
- How to keep infrastructure concerns (hashing, JWT) out of the domain using Application-layer interfaces
- Why Address is a child entity even though it "feels like" a value object (has identity)
- The "same error for both cases" security pattern in authentication
