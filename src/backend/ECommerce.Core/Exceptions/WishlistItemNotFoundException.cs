namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when a wishlist item is not found.
/// </summary>
public sealed class WishlistItemNotFoundException : NotFoundException
{
    public WishlistItemNotFoundException()
        : base("Product not in wishlist") { }

    public WishlistItemNotFoundException(Guid userId, Guid productId)
        : base($"Product {productId} not found in user {userId}'s wishlist") { }
}
