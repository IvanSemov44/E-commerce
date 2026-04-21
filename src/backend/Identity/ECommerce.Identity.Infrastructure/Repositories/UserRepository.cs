using ECommerce.Identity.Domain.Aggregates.User;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.Identity.Infrastructure.Persistence;
using ECommerce.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Identity.Infrastructure.Repositories;

public class UserRepository(IdentityDbContext _db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => LoadUser().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
        => LoadUser().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailResult = Email.Create(email);
        if (!emailResult.IsSuccess) return Task.FromResult(false);
        var emailVo = emailResult.GetDataOrThrow();
        return _db.Users.AnyAsync(u => u.Email == emailVo, cancellationToken);
    }

    public Task<int> GetCustomersCountAsync(CancellationToken cancellationToken = default)
        => _db.Users.CountAsync(u => u.Role == UserRole.Customer, cancellationToken);

    public Task<User?> GetByRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
        => LoadUser().FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token), cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        => await _db.Users.AddAsync(user, cancellationToken);

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        // EF Core change tracking detects mutations on loaded entities automatically.
        // All mutating handlers load before they mutate, so SaveChangesAsync picks up the diff.
        return Task.CompletedTask;
    }

    public Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        _db.Users.Remove(user);
        return Task.CompletedTask;
    }

    private IQueryable<User> LoadUser() =>
        _db.Users
            .Include(u => u.Addresses)
            .Include(u => u.RefreshTokens);
}
