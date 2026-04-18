using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shopping.Infrastructure.Persistence.Repositories;

public class CartRepository(ShoppingDbContext _db) : ICartRepository
{
    public Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId, ct);

    public Task<Cart?> GetByIdAsync(Guid cartId, CancellationToken ct = default)
        => _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == cartId, ct);

    public Task<Cart?> GetBySessionIdAsync(string sessionId, CancellationToken ct = default)
        => _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.SessionId == sessionId, ct);

    public async Task<Cart> GetOrCreateForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (cart is not null) return cart;
        cart = Cart.Create(userId);
        await _db.Carts.AddAsync(cart, ct);
        return cart;
    }

    public async Task<Cart> GetOrCreateForSessionAsync(string sessionId, CancellationToken ct = default)
    {
        var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.SessionId == sessionId, ct);
        if (cart is not null) return cart;
        cart = Cart.CreateAnonymous(sessionId);
        await _db.Carts.AddAsync(cart, ct);
        return cart;
    }

    public Task DeleteAsync(Cart cart, CancellationToken ct = default)
    {
        _db.Carts.Remove(cart);
        return Task.CompletedTask;
    }
}
