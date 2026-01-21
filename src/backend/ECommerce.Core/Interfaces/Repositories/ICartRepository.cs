using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

public interface ICartRepository : IRepository<Cart>
{
    Task<Cart?> GetByUserIdAsync(Guid userId);
    Task<Cart?> GetBySessionIdAsync(string sessionId);
    Task<Cart?> GetCartWithItemsAsync(Guid cartId);
    Task<bool> CartExistsForUserAsync(Guid userId);
    Task<decimal> CalculateTotalAsync(Guid cartId);
    Task<int> GetCartItemCountAsync(Guid cartId);
}
