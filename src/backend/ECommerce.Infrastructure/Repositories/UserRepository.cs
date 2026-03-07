using System.Threading;
using ECommerce.Core.Entities;
using ECommerce.Core.Enums;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for User entity providing data access operations.
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public override Task<User?> GetByIdAsync(Guid id, bool trackChanges = true, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return query
            .Include(u => u.Addresses)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <summary>
    /// Retrieves a user by email address.
    /// </summary>
    public Task<User?> GetByEmailAsync(string email, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return query.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    /// <summary>
    /// Retrieves a user with all their addresses by user ID.
    /// </summary>
    public Task<User?> GetWithAddressesAsync(Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return query
            .Include(u => u.Addresses)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    /// <summary>
    /// Checks if an email address exists in the system.
    /// </summary>
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(u => u.Email == email, cancellationToken);

    /// <summary>
    /// Retrieves a user by Google ID for OAuth authentication.
    /// </summary>
    public Task<User?> GetByGoogleIdAsync(string googleId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return query.FirstOrDefaultAsync(u => u.GoogleId == googleId, cancellationToken);
    }

    /// <summary>
    /// Retrieves a user by Facebook ID for OAuth authentication.
    /// </summary>
    public Task<User?> GetByFacebookIdAsync(string facebookId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return query.FirstOrDefaultAsync(u => u.FacebookId == facebookId, cancellationToken);
    }

    /// <summary>
    /// Gets the total count of customers in the system.
    /// </summary>
    public Task<int> GetCustomersCountAsync(CancellationToken cancellationToken = default)
        => DbSet.CountAsync(u => u.Role == UserRole.Customer, cancellationToken);
}
