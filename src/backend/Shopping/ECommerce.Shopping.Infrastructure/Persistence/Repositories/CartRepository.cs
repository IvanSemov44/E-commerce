using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Interfaces;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shopping.Infrastructure.Persistence.Repositories;

public class CartRepository(AppDbContext _db) : ICartRepository
{
    public async Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.Database
            .SqlQueryRaw<CartRow>(
                "SELECT c.\"Id\", c.\"UserId\", c.\"RowVersion\" FROM \"Carts\" c WHERE c.\"UserId\" = {0}",
                userId)
            .ToListAsync(ct);

        if (rows.Count == 0) return null;

        var cart = Cart.Create(userId);
        var cartId = rows[0].Id;
        
        var items = await _db.Database
            .SqlQueryRaw<CartItemRow>(
                "SELECT \"Id\", \"CartId\", \"ProductId\", \"Quantity\", \"UnitPrice\", \"Currency\" FROM \"CartItems\" WHERE \"CartId\" = {0}",
                cartId)
            .ToListAsync(ct);

        return cart;
    }

    public async Task<Cart?> GetByIdAsync(Guid cartId, CancellationToken ct = default)
    {
        var rows = await _db.Database
            .SqlQueryRaw<CartRow>(
                "SELECT \"Id\", \"UserId\", \"RowVersion\" FROM \"Carts\" WHERE \"Id\" = {0}",
                cartId)
            .ToListAsync(ct);

        if (rows.Count == 0) return null;

        return Cart.Create(rows[0].UserId);
    }

    public async Task UpsertAsync(Cart cart, CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlRawAsync(
            @"INSERT INTO ""Carts"" (""Id"", ""UserId"", ""RowVersion"") VALUES ({0}, {1}, {2}) 
              ON CONFLICT (""Id"") DO UPDATE SET ""UserId"" = {1}",
            cart.Id, cart.UserId, cart.RowVersion);

        foreach (var item in cart.Items)
        {
            await _db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO ""CartItems"" (""Id"", ""CartId"", ""ProductId"", ""Quantity"", ""UnitPrice"", ""Currency"") VALUES ({0}, {1}, {2}, {3}, {4}, {5})
                  ON CONFLICT (""Id"") DO UPDATE SET ""Quantity"" = {3}",
                item.Id, item.CartId, item.ProductId, item.Quantity, item.UnitPrice, item.Currency);
        }
    }

    public Task DeleteAsync(Cart cart, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    private record CartRow(Guid Id, Guid UserId, byte[] RowVersion);
    private record CartItemRow(Guid Id, Guid CartId, Guid ProductId, int Quantity, decimal UnitPrice, string Currency);
}