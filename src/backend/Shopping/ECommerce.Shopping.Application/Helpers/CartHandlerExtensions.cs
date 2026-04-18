using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Helpers;

public static class CartHandlerExtensions
{
    public static async Task<Cart?> ResolveCartAsync(
        this ICartRepository carts,
        Guid? userId,
        string? sessionId,
        CancellationToken ct)
    {
        if (userId is Guid uid)
            return await carts.GetOrCreateForUserAsync(uid, ct);

        if (sessionId is not null)
            return await carts.GetOrCreateForSessionAsync(sessionId, ct);

        return null;
    }
}
