using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Helpers;

public static class CartHandlerExtensions
{
    /// <summary>
    /// Resolves a cart based on userId or sessionId.
    /// If userId is provided, loads or creates a user cart.
    /// If sessionId is provided, loads or creates an anonymous (session-based) cart.
    /// Returns null if neither userId nor sessionId is provided.
    /// </summary>
    public static async Task<Cart?> ResolveCartAsync(
        this ICartRepository carts,
        Guid? userId,
        string? sessionId,
        CancellationToken ct)
    {
        if (userId is Guid uid)
        {
            var userCart = await carts.GetByUserIdAsync(uid, ct);
            return userCart ?? Cart.Create(uid);
        }

        if (sessionId is not null)
        {
            var sessionCart = await carts.GetBySessionIdAsync(sessionId, ct);
            return sessionCart ?? Cart.CreateAnonymous(sessionId);
        }

        return null;
    }
}
