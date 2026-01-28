using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetWithAddressesAsync(Guid userId);
    Task<bool> EmailExistsAsync(string email);
    Task<User?> GetByGoogleIdAsync(string googleId);
    Task<User?> GetByFacebookIdAsync(string facebookId);
    Task<int> GetCustomersCountAsync();
}
