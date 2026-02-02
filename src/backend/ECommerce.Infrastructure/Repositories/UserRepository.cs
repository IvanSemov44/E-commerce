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

    /// <summary>
    /// Retrieves a user by email address.
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    /// <summary>
    /// Retrieves a user with all their addresses by user ID.
    /// </summary>
    public async Task<User?> GetWithAddressesAsync(Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Include(u => u.Addresses)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    /// <summary>
    /// Checks if an email address exists in the system.
    /// </summary>
    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(u => u.Email == email, cancellationToken);
    }

    /// <summary>
    /// Retrieves a user by Google ID for OAuth authentication.
    /// </summary>
    public async Task<User?> GetByGoogleIdAsync(string googleId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query.FirstOrDefaultAsync(u => u.GoogleId == googleId, cancellationToken);
    }

    /// <summary>
    /// Retrieves a user by Facebook ID for OAuth authentication.
    /// </summary>
    public async Task<User?> GetByFacebookIdAsync(string facebookId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query.FirstOrDefaultAsync(u => u.FacebookId == facebookId, cancellationToken);
    }

    /// <summary>
    /// Gets the total count of customers in the system.
    /// </summary>
    public async Task<int> GetCustomersCountAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(u => u.Role == UserRole.Customer, cancellationToken);
    }
}
