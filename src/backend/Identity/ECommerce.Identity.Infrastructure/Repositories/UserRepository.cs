using ECommerce.Identity.Domain.Interfaces;
using ECommerce.SharedKernel.Enums;

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

    private IQueryable<User> LoadUser() =>
        _db.Users
            .Include(u => u.Addresses)
            .Include(u => u.RefreshTokens);
}
