using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Interfaces;
using ECommerce.Shopping.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using CoreCart = ECommerce.Core.Entities.Cart;
using CoreCartItem = ECommerce.Core.Entities.CartItem;

namespace ECommerce.Shopping.Infrastructure.Persistence.Repositories;

public class CartRepository(ShoppingDbContext _db) : ICartRepository
{
    public async Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        return cart is null ? null : MapToDomain(cart);
    }

    public async Task<Cart?> GetByIdAsync(Guid cartId, CancellationToken ct = default)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId, ct);

        return cart is null ? null : MapToDomain(cart);
    }

    public async Task<Cart?> GetBySessionIdAsync(string sessionId, CancellationToken ct = default)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId, ct);

        return cart is null ? null : MapToDomain(cart);
    }

    public async Task UpsertAsync(Cart cart, CancellationToken ct = default)
    {
        var existing = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cart.Id, ct);

        if (existing is null)
        {
            var newCart = new CoreCart
            {
                Id = cart.Id,
                UserId = cart.UserId == Guid.Empty ? null : cart.UserId,
                SessionId = cart.SessionId,
                RowVersion = cart.RowVersion
            };
            await _db.Carts.AddAsync(newCart, ct);
        }
        else
        {
            existing.UserId = cart.UserId == Guid.Empty ? null : cart.UserId;
            existing.SessionId = cart.SessionId;

            _db.CartItems.RemoveRange(existing.Items);

            foreach (var item in cart.Items)
            {
                _db.CartItems.Add(new CoreCartItem
                {
                    Id = item.Id,
                    CartId = cart.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Currency = item.Currency
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Cart cart, CancellationToken ct = default)
    {
        var existing = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cart.Id, ct);

        if (existing is not null)
        {
            _db.Carts.Remove(existing);
            await _db.SaveChangesAsync(ct);
        }
    }

    private static Cart MapToDomain(CoreCart cart)
    {
        // Determine cart type based on UserId and SessionId
        Cart domainCart = cart.SessionId is not null
            ? Cart.CreateAnonymousWithId(cart.Id, cart.SessionId)
            : Cart.CreateWithId(cart.Id, cart.UserId ?? Guid.Empty);

        foreach (var item in cart.Items)
        {
            domainCart.AddItem(item.ProductId, item.Quantity, item.UnitPrice, item.Currency);
        }

        return domainCart;
    }
}
