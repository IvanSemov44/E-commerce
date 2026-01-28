using ECommerce.Core.Entities;
using ECommerce.Core.Enums;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetWithAddressesAsync(Guid userId)
    {
        return await DbSet
            .Include(u => u.Addresses)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await DbSet.AnyAsync(u => u.Email == email);
    }

    public async Task<User?> GetByGoogleIdAsync(string googleId)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.GoogleId == googleId);
    }

    public async Task<User?> GetByFacebookIdAsync(string facebookId)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.FacebookId == facebookId);
    }

    public async Task<int> GetCustomersCountAsync()
    {
        return await DbSet.CountAsync(u => u.Role == UserRole.Customer);
    }
}
