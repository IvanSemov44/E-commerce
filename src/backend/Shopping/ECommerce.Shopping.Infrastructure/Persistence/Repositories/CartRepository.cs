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

    public async Task UpsertAsync(Cart cart, CancellationToken ct = default)
    {
        var existing = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cart.Id, ct);

        if (existing is not null)
            _db.Carts.Remove(existing);

        await _db.Carts.AddAsync(cart, ct);
    }

    public async Task DeleteAsync(Cart cart, CancellationToken ct = default)
    {
        var existing = await _db.Carts
            .FirstOrDefaultAsync(c => c.Id == cart.Id, ct);

        if (existing is not null)
            _db.Carts.Remove(existing);
    }
}
