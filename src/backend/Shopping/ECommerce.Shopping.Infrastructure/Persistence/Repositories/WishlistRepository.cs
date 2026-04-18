using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using ECommerce.Shopping.Domain.Interfaces;
using ECommerce.Shopping.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shopping.Infrastructure.Persistence.Repositories;

public class WishlistRepository(ShoppingDbContext _db) : IWishlistRepository
{
    public Task<Wishlist?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _db.Wishlists.FirstOrDefaultAsync(w => w.UserId == userId, ct);

    public async Task UpsertAsync(Wishlist wishlist, CancellationToken ct = default)
    {
        var exists = await _db.Wishlists.AnyAsync(w => w.Id == wishlist.Id, ct);

        if (exists)
            _db.Wishlists.Update(wishlist);
        else
            await _db.Wishlists.AddAsync(wishlist, ct);
    }
}
