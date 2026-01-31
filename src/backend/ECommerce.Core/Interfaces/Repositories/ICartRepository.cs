using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

public interface ICartRepository : IRepository<Cart>
{
    Task<Cart?> GetByUserIdAsync(Guid userId, bool trackChanges = false);
    Task<Cart?> GetBySessionIdAsync(string sessionId, bool trackChanges = false);
    Task<Cart?> GetCartWithItemsAsync(Guid cartId, bool trackChanges = false);
    Task<bool> CartExistsForUserAsync(Guid userId);
    Task<decimal> CalculateTotalAsync(Guid cartId);
    Task<int> GetCartItemCountAsync(Guid cartId);
}
