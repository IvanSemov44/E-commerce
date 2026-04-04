using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using ECommerce.Shopping.Domain.Interfaces;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shopping.Infrastructure.Persistence.Repositories;

public class WishlistRepository(AppDbContext _db) : IWishlistRepository
{
    public async Task<Wishlist?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.Wishlists
            .Where(w => w.UserId == userId)
            .ToListAsync(ct);

        if (rows.Count == 0) return null;

        var wishlist = Wishlist.Create(userId);
        foreach (var row in rows)
            wishlist.AddProduct(row.ProductId);

        return wishlist;
    }

    public async Task UpsertAsync(Wishlist wishlist, CancellationToken ct = default)
    {
        var existing = await _db.Wishlists
            .Where(w => w.UserId == wishlist.UserId)
            .ToListAsync(ct);

        _db.Wishlists.RemoveRange(existing);

        foreach (var productId in wishlist.ProductIds)
        {
            await _db.Wishlists.AddAsync(new Core.Entities.Wishlist
            {
                Id = Guid.NewGuid(),
                UserId = wishlist.UserId,
                ProductId = productId
            }, ct);
        }

        await _db.SaveChangesAsync(ct);
    }
}