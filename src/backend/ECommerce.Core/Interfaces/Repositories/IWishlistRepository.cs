using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

public interface IWishlistRepository : IRepository<Wishlist>
{
    Task<Wishlist?> GetByUserIdAsync(Guid userId);
    Task<Wishlist?> GetByUserIdWithItemsAsync(Guid userId);
    Task<bool> IsProductInWishlistAsync(Guid userId, Guid productId);
    Task<int> GetWishlistItemCountAsync(Guid userId);
    Task<Wishlist?> GetOrCreateForUserAsync(Guid userId);
}
