# Phase 2, Step 3: Identity Infrastructure Project

**Prerequisite**: Step 2 (`ECommerce.Identity.Application`) is complete and `dotnet build` passes.

---

## Task: Create ECommerce.Identity.Infrastructure Project

### 1. Create the project

```bash
cd src/backend
dotnet new classlib -n ECommerce.Identity.Infrastructure -f net10.0 -o Identity/ECommerce.Identity.Infrastructure
dotnet sln ../../ECommerce.sln add Identity/ECommerce.Identity.Infrastructure/ECommerce.Identity.Infrastructure.csproj

dotnet add Identity/ECommerce.Identity.Infrastructure/ECommerce.Identity.Infrastructure.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
dotnet add Identity/ECommerce.Identity.Infrastructure/ECommerce.Identity.Infrastructure.csproj \
    reference Identity/ECommerce.Identity.Domain/ECommerce.Identity.Domain.csproj
dotnet add Identity/ECommerce.Identity.Infrastructure/ECommerce.Identity.Infrastructure.csproj \
    reference Identity/ECommerce.Identity.Application/ECommerce.Identity.Application.csproj
dotnet add Identity/ECommerce.Identity.Infrastructure/ECommerce.Identity.Infrastructure.csproj \
    reference ECommerce.Infrastructure/ECommerce.Infrastructure.csproj  # for AppDbContext

dotnet add Identity/ECommerce.Identity.Infrastructure/ECommerce.Identity.Infrastructure.csproj \
    package Microsoft.EntityFrameworkCore
dotnet add Identity/ECommerce.Identity.Infrastructure/ECommerce.Identity.Infrastructure.csproj \
    package BCrypt.Net-Next
dotnet add Identity/ECommerce.Identity.Infrastructure/ECommerce.Identity.Infrastructure.csproj \
    package Microsoft.IdentityModel.Tokens
dotnet add Identity/ECommerce.Identity.Infrastructure/ECommerce.Identity.Infrastructure.csproj \
    package System.IdentityModel.Tokens.Jwt
dotnet add Identity/ECommerce.Identity.Infrastructure/ECommerce.Identity.Infrastructure.csproj \
    package Microsoft.AspNetCore.Authentication.JwtBearer

rm Identity/ECommerce.Identity.Infrastructure/Class1.cs
```

### 2. Implement IPasswordHasher

Look at the existing `AuthService.cs` — it has `HashPassword(string)` and verifies with BCrypt. Port that logic here.

**File: `Identity/ECommerce.Identity.Infrastructure/Services/PasswordHasher.cs`**

```csharp
using ECommerce.Identity.Application.Interfaces;

namespace ECommerce.Identity.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string rawPassword)
        => BCrypt.Net.BCrypt.HashPassword(rawPassword, workFactor: 12);

    public bool Verify(string rawPassword, string hash)
        => BCrypt.Net.BCrypt.Verify(rawPassword, hash);
}
```

### 3. Implement IJwtTokenService

Look at `AuthService.GenerateJwtToken()` and `GenerateRefreshToken()` for the existing implementation. Port the JWT logic here.

**File: `Identity/ECommerce.Identity.Infrastructure/Services/JwtTokenService.cs`**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Aggregates.User;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Identity.Infrastructure.Services;

public class JwtTokenService(IConfiguration _config) : IJwtTokenService
{
    public string GenerateAccessToken(User user)
    {
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpiryMinutes"] ?? "60"));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email.Value),
            new Claim(ClaimTypes.Role,               user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"],
            audience:           _config["Jwt:Audience"],
            claims:             claims,
            expires:            expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
```

### 4. Implement UserRepository

The repository maps between `ECommerce.Core.Entities.User` (the EF/DB entity) and `ECommerce.Identity.Domain.Aggregates.User.User` (the domain aggregate). This is the same bridge pattern used in the Catalog phase.

**File: `Identity/ECommerce.Identity.Infrastructure/Repositories/UserRepository.cs`**

```csharp
using ECommerce.Identity.Domain.Aggregates.User;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using CoreUser = ECommerce.Core.Entities.User;

namespace ECommerce.Identity.Infrastructure.Repositories;

public class UserRepository(AppDbContext _db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var core = await _db.Users
            .Include(u => u.Addresses)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return core is null ? null : MapToDomain(core);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        var core = await _db.Users
            .Include(u => u.Addresses)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == email.Value, cancellationToken);
        return core is null ? null : MapToDomain(core);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        => _db.Users.AnyAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

    public async Task<User?> GetByRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var core = await _db.Users
            .Include(u => u.Addresses)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token), cancellationToken);
        return core is null ? null : MapToDomain(core);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        var core = MapToCore(user);
        await _db.Users.AddAsync(core, cancellationToken);
    }

    public Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        var core = _db.Users.Local.FirstOrDefault(u => u.Id == user.Id)
                   ?? new CoreUser { Id = user.Id };
        _db.Users.Remove(core);
        return Task.CompletedTask;
    }

    // ── Mapping ────────────────────────────────────────────────────────────────

    private static User MapToDomain(CoreUser core)
    {
        // Use reflection helper or a dedicated factory on the aggregate.
        // Here we reconstitute via the internal EF constructor approach.
        // NOTE: If User has no public reconstitution factory, add one:
        //   public static User Reconstitute(Guid id, Email email, PersonName name, ...)
        // and call it here. Adjust to match whatever reconstitution pattern you added.
        throw new NotImplementedException(
            "Implement MapToDomain after deciding on reconstitution strategy. " +
            "Options: (a) Add User.Reconstitute() static factory, " +
            "(b) Use a separate read model for queries, " +
            "(c) Let EF map directly to the domain aggregate (requires OwnsOne/OwnsMany config).");
    }

    private static CoreUser MapToCore(User domain)
    {
        // Map domain aggregate properties back to the EF core entity.
        // This will need to be fleshed out based on the CoreUser entity shape.
        throw new NotImplementedException("Implement MapToCore.");
    }
}
```

> **Important note on reconstitution**: In Phase 1 (Catalog), we used `MapToDomain` to manually reconstruct domain objects from core entities. You have two options for Phase 2:
>
> **Option A (same as Phase 1)**: Add `User.Reconstitute(id, email, name, passwordHash, ...)` static factory to the domain aggregate. The infrastructure calls it directly. This keeps EF out of the domain.
>
> **Option B (simpler)**: Configure EF to map directly to the domain `User` class using `OwnsOne`/`OwnsMany` and `HasConversion` (see Step 3 EF config below), then let `_db.Users` be typed as `DbSet<Identity.Domain.Aggregates.User.User>` — but this requires changing the DbContext, which risks breaking existing Auth. Use Option A for safety in this phase.
>
> **Recommended**: Use Option A. Add `Reconstitute()` to the `User` aggregate in step-1-domain work.

### 5. EF Core configurations

These configurations tell EF how to persist the Identity domain aggregate using the EXISTING `Users` table. They are applied to the existing `AppDbContext` — do NOT create a new DbContext in this phase.

**File: `Identity/ECommerce.Identity.Infrastructure/Configurations/UserConfiguration.cs`**

```csharp
using ECommerce.Identity.Domain.Aggregates.User;
using ECommerce.Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Identity.Infrastructure.Configurations;

// Only apply this if EF is mapping directly to the domain User class (Option B above).
// If using Option A (CoreUser + MapToDomain), skip this file and keep existing UserConfiguration.
public class IdentityUserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        // Email: single-property VO → HasConversion (one column)
        builder.Property(u => u.Email)
            .HasConversion(e => e.Value, v => Email.Create(v).GetDataOrThrow())
            .HasColumnName("Email")
            .IsRequired()
            .HasMaxLength(256);
        builder.HasIndex(u => u.Email).IsUnique();

        // PersonName: multi-property VO → OwnsOne (two columns)
        builder.OwnsOne(u => u.Name, nb =>
        {
            nb.Property(n => n.First).HasColumnName("FirstName").IsRequired().HasMaxLength(100);
            nb.Property(n => n.Last).HasColumnName("LastName").IsRequired().HasMaxLength(100);
        });

        // PasswordHash: single-property VO → HasConversion
        builder.Property(u => u.PasswordHash)
            .HasConversion(p => p.Hash, v => PasswordHash.FromHash(v).GetDataOrThrow())
            .HasColumnName("PasswordHash")
            .IsRequired();

        builder.Property(u => u.PhoneNumber).HasColumnName("PhoneNumber").IsRequired(false);
        builder.Property(u => u.Role).HasConversion<string>().HasColumnName("Role"); // Rule 45: enums stored as strings
        builder.Property(u => u.IsEmailVerified).HasColumnName("IsEmailVerified");
        builder.Property(u => u.EmailVerificationToken).HasColumnName("EmailVerificationToken").IsRequired(false);
        builder.Property(u => u.PasswordResetToken).HasColumnName("PasswordResetToken").IsRequired(false);
        builder.Property(u => u.PasswordResetExpiry).HasColumnName("PasswordResetExpiry").IsRequired(false);

        // Addresses: child entities → OwnsMany
        builder.OwnsMany(u => u.Addresses, ab =>
        {
            ab.ToTable("UserAddresses");
            ab.HasKey(a => a.Id);
            ab.Property(a => a.Street).IsRequired().HasMaxLength(200);
            ab.Property(a => a.City).IsRequired().HasMaxLength(100);
            ab.Property(a => a.Country).IsRequired().HasMaxLength(100);
            ab.Property(a => a.PostalCode).IsRequired(false).HasMaxLength(20);
            ab.Property(a => a.IsDefaultShipping);
            ab.Property(a => a.IsDefaultBilling);
            ab.WithOwner().HasForeignKey("UserId");
        });

        // RefreshTokens: child entities → OwnsMany
        builder.OwnsMany(u => u.RefreshTokens, rb =>
        {
            rb.ToTable("UserRefreshTokens");
            rb.HasKey(t => t.Id);
            rb.Property(t => t.Token).IsRequired().HasMaxLength(500);
            rb.Property(t => t.ExpiresAt);
            rb.Property(t => t.CreatedAt);
            rb.Property(t => t.IsRevoked);
            rb.Property(t => t.RevokedReason).IsRequired(false).HasMaxLength(200);
            rb.WithOwner().HasForeignKey("UserId");
        });
    }
}
```

### 6. DI registration

**File: `Identity/ECommerce.Identity.Infrastructure/DependencyInjection.cs`**

```csharp
using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Infrastructure.Repositories;
using ECommerce.Identity.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        return services;
    }
}
```

### 7. Register in API

In `src/backend/ECommerce.API/ECommerce.API.csproj`, add project reference:

```bash
cd src/backend
dotnet add ECommerce.API/ECommerce.API.csproj \
    reference Identity/ECommerce.Identity.Infrastructure/ECommerce.Identity.Infrastructure.csproj
```

In `src/backend/ECommerce.API/Program.cs`, add:

```csharp
using ECommerce.Identity.Infrastructure;

// After AddCatalogInfrastructure():
builder.Services.AddIdentityInfrastructure();
```

### 8. Create database migration

The EF config introduces two new tables (`UserAddresses`, `UserRefreshTokens`) and new columns on `Users` (`PasswordResetToken`, `PasswordResetExpiry`). Check if these already exist in the existing schema before creating a migration.

```bash
cd src/backend

# Check what the existing Users table looks like:
# grep for existing AddressConfiguration or RefreshToken in the current EF config
grep -r "UserAddresses\|UserRefreshTokens\|PasswordResetToken" ECommerce.Infrastructure/ --include="*.cs"

# If new tables/columns are needed, create a migration:
dotnet ef migrations add AddIdentityDomainTables \
    --project ECommerce.Infrastructure/ECommerce.Infrastructure.csproj \
    --startup-project ECommerce.API/ECommerce.API.csproj

# Review the generated migration file — confirm it only adds new tables/columns,
# does NOT drop or rename existing columns.
# Then apply:
dotnet ef database update \
    --project ECommerce.Infrastructure/ECommerce.Infrastructure.csproj \
    --startup-project ECommerce.API/ECommerce.API.csproj
```

> See `.ai/workflows/database-migrations.md` for the full migration safety checklist.

### 9. Verify

```bash
cd src/backend
dotnet build Identity/ECommerce.Identity.Infrastructure/ECommerce.Identity.Infrastructure.csproj
dotnet build
dotnet test  # All existing tests still pass
```

---

## Acceptance Criteria

- [ ] `ECommerce.Identity.Infrastructure` project created and added to solution
- [ ] `PasswordHasher` implements `IPasswordHasher` using BCrypt
- [ ] `JwtTokenService` implements `IJwtTokenService` using the same JWT config as existing `AuthService`
- [ ] `UserRepository` implements `IUserRepository` (MapToDomain reconstitution strategy chosen and implemented)
- [ ] EF configurations handle `Email` via `HasConversion`, `PersonName` via `OwnsOne`, `Address`/`RefreshToken` via `OwnsMany`
- [ ] `AddIdentityInfrastructure()` extension method registers all three services
- [ ] DI registration added to Program.cs
- [ ] `dotnet build` passes
- [ ] All existing integration tests still pass (`dotnet test`)
