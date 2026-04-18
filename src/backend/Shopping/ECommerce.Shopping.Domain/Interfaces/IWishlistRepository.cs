using ECommerce.Shopping.Domain.Aggregates.Wishlist;

namespace ECommerce.Shopping.Domain.Interfaces;

public interface IWishlistRepository
{
    Task<Wishlist?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Wishlist> GetOrCreateForUserAsync(Guid userId, CancellationToken ct = default);
}
