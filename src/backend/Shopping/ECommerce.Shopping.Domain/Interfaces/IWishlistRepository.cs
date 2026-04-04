using ECommerce.Shopping.Domain.Aggregates.Wishlist;

namespace ECommerce.Shopping.Domain.Interfaces;

public interface IWishlistRepository
{
    Task<Wishlist?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task UpsertAsync(Wishlist wishlist, CancellationToken ct = default);
}