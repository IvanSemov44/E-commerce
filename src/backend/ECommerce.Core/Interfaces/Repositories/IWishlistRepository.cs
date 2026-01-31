using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

public interface IWishlistRepository : IRepository<Wishlist>
{
    Task<Wishlist?> GetByUserIdAsync(Guid userId, bool trackChanges = false);
    Task<Wishlist?> GetByUserIdWithItemsAsync(Guid userId, bool trackChanges = false);
    Task<bool> IsProductInWishlistAsync(Guid userId, Guid productId);
    Task<int> GetWishlistItemCountAsync(Guid userId);
}
