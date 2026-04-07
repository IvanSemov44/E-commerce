using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using CoreUser         = ECommerce.Core.Entities.User;
using CoreAddress      = ECommerce.Core.Entities.Address;
using CoreRefreshToken = ECommerce.Core.Entities.RefreshToken;
using DomainUser       = ECommerce.Identity.Domain.Aggregates.User.User;

namespace ECommerce.Identity.Infrastructure.Repositories;

public class UserRepository(IdentityDbContext _db) : IUserRepository
{
    // ── Queries ────────────────────────────────────────────────────────────────

    public async Task<DomainUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var core = await LoadCoreUser()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return core is null ? null : MapToDomain(core);
    }

    public async Task<DomainUser?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        var core = await LoadCoreUser()
            .FirstOrDefaultAsync(u => u.Email == email.Value, cancellationToken);
        return core is null ? null : MapToDomain(core);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        => _db.Users.AnyAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

    public Task<int> GetCustomersCountAsync(CancellationToken cancellationToken = default)
        => _db.Users.CountAsync(u => u.Role == ECommerce.Core.Enums.UserRole.Customer, cancellationToken);

    public async Task<DomainUser?> GetByRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var core = await LoadCoreUser()
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token), cancellationToken);
        return core is null ? null : MapToDomain(core);
    }

    // ── Commands ───────────────────────────────────────────────────────────────

    public async Task AddAsync(DomainUser user, CancellationToken cancellationToken = default)
    {
        var core = MapToCore(user);
        await _db.Users.AddAsync(core, cancellationToken);
    }

    public async Task UpdateAsync(DomainUser user, CancellationToken cancellationToken = default)
    {
        // Check if the entity is already tracked (e.g., from GetByEmailAsync/GetByIdAsync)
        var tracked = _db.Users.Local.FirstOrDefault(u => u.Id == user.Id);
        if (tracked is not null)
        {
            // Entity is already tracked - just update scalar properties.
            // EF Core's change tracking will persist the changes on SaveChanges.
            tracked.Email             = user.Email.Value;
            tracked.FirstName         = user.Name.First;
            tracked.LastName          = user.Name.Last;
            tracked.PasswordHash      = user.PasswordHash.Hash;
            tracked.Phone             = user.PhoneNumber;
            tracked.Role              = (ECommerce.Core.Enums.UserRole)(int)user.Role;
            tracked.IsEmailVerified   = user.IsEmailVerified;
            tracked.EmailVerificationToken = user.EmailVerificationToken;
            tracked.PasswordResetToken     = user.PasswordResetToken;
            tracked.PasswordResetExpires   = user.PasswordResetExpiry;
            return;
        }

        // Entity not tracked - query from database
        var existing = await _db.Users
            .Include(u => u.Addresses)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken);

        if (existing is null)
        {
            await AddAsync(user, cancellationToken);
            return;
        }

        // Scalar properties
        existing.Email             = user.Email.Value;
        existing.FirstName         = user.Name.First;
        existing.LastName          = user.Name.Last;
        existing.PasswordHash      = user.PasswordHash.Hash;
        existing.Phone             = user.PhoneNumber;
        existing.Role              = (ECommerce.Core.Enums.UserRole)(int)user.Role;
        existing.IsEmailVerified   = user.IsEmailVerified;
        existing.EmailVerificationToken = user.EmailVerificationToken;
        existing.PasswordResetToken     = user.PasswordResetToken;
        existing.PasswordResetExpires   = user.PasswordResetExpiry;

        // Sync refresh tokens
        var existingTokenValues = existing.RefreshTokens.Select(t => t.Token).ToHashSet();
        var domainTokenValues   = user.RefreshTokens.Select(t => t.Token).ToHashSet();

        var toRemove = existing.RefreshTokens.Where(t => !domainTokenValues.Contains(t.Token)).ToList();
        foreach (var rt in toRemove)
            existing.RefreshTokens.Remove(rt);

        foreach (var domainRt in user.RefreshTokens)
        {
            var existingRt = existing.RefreshTokens.FirstOrDefault(t => t.Token == domainRt.Token);
            if (existingRt is not null)
            {
                existingRt.IsRevoked = domainRt.IsRevoked;
            }
            else if (!existingTokenValues.Contains(domainRt.Token))
            {
                existing.RefreshTokens.Add(new CoreRefreshToken
                {
                    Id        = domainRt.Id,
                    UserId    = user.Id,
                    Token     = domainRt.Token,
                    ExpiresAt = domainRt.ExpiresAt,
                    IsRevoked = domainRt.IsRevoked,
                });
            }
        }

        // Sync addresses
        var existingAddressIds = existing.Addresses.Select(a => a.Id).ToHashSet();
        var domainAddressIds   = user.Addresses.Select(a => a.Id).ToHashSet();

        var addressesToRemove = existing.Addresses.Where(a => !domainAddressIds.Contains(a.Id)).ToList();
        foreach (var a in addressesToRemove)
            existing.Addresses.Remove(a);

        foreach (var domainAddr in user.Addresses)
        {
            var existingAddr = existing.Addresses.FirstOrDefault(a => a.Id == domainAddr.Id);
            if (existingAddr is not null)
            {
                existingAddr.StreetLine1 = domainAddr.Street;
                existingAddr.City        = domainAddr.City;
                existingAddr.Country     = domainAddr.Country[..Math.Min(2, domainAddr.Country.Length)];
                existingAddr.PostalCode  = domainAddr.PostalCode ?? string.Empty;
                existingAddr.IsDefault   = domainAddr.IsDefaultShipping;
            }
            else
            {
                existing.Addresses.Add(new CoreAddress
                {
                    Id          = domainAddr.Id,
                    UserId      = user.Id,
                    Type        = "Shipping",
                    FirstName   = existing.FirstName,
                    LastName    = existing.LastName,
                    StreetLine1 = domainAddr.Street,
                    City        = domainAddr.City,
                    State       = string.Empty,
                    PostalCode  = domainAddr.PostalCode ?? string.Empty,
                    Country     = domainAddr.Country[..Math.Min(2, domainAddr.Country.Length)],
                    IsDefault   = domainAddr.IsDefaultShipping,
                });
            }
        }
    }

    public Task DeleteAsync(DomainUser user, CancellationToken cancellationToken = default)
    {
        var core = _db.Users.Local.FirstOrDefault(u => u.Id == user.Id)
                   ?? new CoreUser { Id = user.Id };
        _db.Users.Remove(core);
        return Task.CompletedTask;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private IQueryable<CoreUser> LoadCoreUser() =>
        _db.Users
            .Include(u => u.Addresses)
            .Include(u => u.RefreshTokens);

    // ── Mapping: Core → Domain ─────────────────────────────────────────────────

    private static DomainUser MapToDomain(CoreUser core)
    {
        var email        = Email.Create(core.Email).GetDataOrThrow();
        var name         = PersonName.Create(core.FirstName, core.LastName).GetDataOrThrow();
        var passwordHash = PasswordHash.FromHash(core.PasswordHash ?? string.Empty).GetDataOrThrow();

        var domain = DomainUser.Reconstitute(
            id:                     core.Id,
            email:                  email,
            name:                   name,
            passwordHash:           passwordHash,
            phoneNumber:            core.Phone,
            role:                   (ECommerce.Identity.Domain.Aggregates.User.UserRole)(int)core.Role,
            isEmailVerified:        core.IsEmailVerified,
            emailVerificationToken: core.EmailVerificationToken,
            passwordResetToken:     core.PasswordResetToken,
            passwordResetExpiry:    core.PasswordResetExpires);

        foreach (var rt in core.RefreshTokens)
        {
            domain.AddReconstitutedRefreshToken(
                id:            rt.Id,
                token:         rt.Token,
                expiresAt:     rt.ExpiresAt,
                createdAt:     rt.CreatedAt,
                isRevoked:     rt.IsRevoked,
                revokedReason: null);
        }

        // Best-effort address mapping: Core Address has richer schema (State, Type, FirstName…)
        // than the Identity domain Address (Street, City, Country, PostalCode, IsDefaultShipping/Billing).
        // TODO Phase 3: add dedicated identity address table to eliminate this mismatch.
        foreach (var a in core.Addresses)
        {
            domain.AddReconstitutedAddress(
                id:               a.Id,
                street:           a.StreetLine1,
                city:             a.City,
                country:          a.Country,
                postalCode:       a.PostalCode,
                isDefaultShipping: a.IsDefault,
                isDefaultBilling:  a.IsDefault);
        }

        return domain;
    }

    // ── Mapping: Domain → Core ─────────────────────────────────────────────────

    private static CoreUser MapToCore(DomainUser domain)
    {
        var core = new CoreUser
        {
            Id                      = domain.Id,
            Email                   = domain.Email.Value,
            FirstName               = domain.Name.First,
            LastName                = domain.Name.Last,
            PasswordHash            = domain.PasswordHash.Hash,
            Phone                   = domain.PhoneNumber,
            Role                    = (ECommerce.Core.Enums.UserRole)(int)domain.Role,
            IsEmailVerified         = domain.IsEmailVerified,
            EmailVerificationToken  = domain.EmailVerificationToken,
            PasswordResetToken      = domain.PasswordResetToken,
            PasswordResetExpires    = domain.PasswordResetExpiry,
            CreatedAt               = domain.CreatedAt,
            UpdatedAt               = domain.UpdatedAt,
        };

        foreach (var rt in domain.RefreshTokens)
        {
            core.RefreshTokens.Add(new CoreRefreshToken
            {
                Id        = rt.Id,
                UserId    = domain.Id,
                Token     = rt.Token,
                ExpiresAt = rt.ExpiresAt,
                IsRevoked = rt.IsRevoked,
            });
        }

        foreach (var a in domain.Addresses)
        {
            core.Addresses.Add(new CoreAddress
            {
                Id          = a.Id,
                UserId      = domain.Id,
                Type        = "Shipping",
                FirstName   = domain.Name.First,
                LastName    = domain.Name.Last,
                StreetLine1 = a.Street,
                City        = a.City,
                State       = string.Empty,
                PostalCode  = a.PostalCode ?? string.Empty,
                Country     = a.Country[..Math.Min(2, a.Country.Length)],
                IsDefault   = a.IsDefaultShipping,
            });
        }

        return core;
    }
}
