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
            .FromSqlRaw(
                "SELECT \"Id\", \"UserId\", \"ProductId\" FROM \"Wishlists\" WHERE \"UserId\" = {0}",
                userId)
            .ToListAsync(ct);

        if (rows.Count == 0) return null;

        var wishlist = Wishlist.Create(userId);
        foreach (var row in rows)
            wishlist.AddProduct(row.ProductId);

        return wishlist;
    }

    public async Task UpsertAsync(Wishlist wishlist, CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlRawAsync(
            "DELETE FROM \"Wishlists\" WHERE \"UserId\" = {0}", wishlist.UserId);

        foreach (var productId in wishlist.ProductIds)
        {
            await _db.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Wishlists\" (\"Id\", \"UserId\", \"ProductId\") VALUES ({0}, {1}, {2})",
                Guid.NewGuid(), wishlist.UserId, productId);
        }
    }
}