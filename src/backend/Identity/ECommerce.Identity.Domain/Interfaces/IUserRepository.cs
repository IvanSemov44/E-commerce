using ECommerce.Identity.Domain.Aggregates.User;
using ECommerce.Identity.Domain.ValueObjects;

namespace ECommerce.Identity.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?>  GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?>  GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<bool>   EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<User?>  GetByRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(User user, CancellationToken cancellationToken = default);
}
