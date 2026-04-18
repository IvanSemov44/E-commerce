using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using ECommerce.Shopping.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shopping.Infrastructure.Persistence.Repositories;

public class WishlistRepository(ShoppingDbContext _db) : IWishlistRepository
{
    public Task<Wishlist?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _db.Wishlists.FirstOrDefaultAsync(w => w.UserId == userId, ct);

    public async Task<Wishlist> GetOrCreateForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var wishlist = await _db.Wishlists.FirstOrDefaultAsync(w => w.UserId == userId, ct);
        if (wishlist is not null) return wishlist;
        wishlist = Wishlist.Create(userId);
        await _db.Wishlists.AddAsync(wishlist, ct);
        return wishlist;
    }
}
