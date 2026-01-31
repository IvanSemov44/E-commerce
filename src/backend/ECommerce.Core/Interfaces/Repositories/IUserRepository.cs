using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, bool trackChanges = false);
    Task<User?> GetWithAddressesAsync(Guid userId, bool trackChanges = false);
    Task<bool> EmailExistsAsync(string email);
    Task<User?> GetByGoogleIdAsync(string googleId, bool trackChanges = false);
    Task<User?> GetByFacebookIdAsync(string facebookId, bool trackChanges = false);
    Task<int> GetCustomersCountAsync();
}
