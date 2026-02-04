using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when attempting to add a product that is already in the wishlist.
/// </summary>
public sealed class DuplicateWishlistItemException : ConflictException
{
    public DuplicateWishlistItemException()
        : base("Product already in wishlist") { }

    public DuplicateWishlistItemException(Guid userId, Guid productId)
        : base($"Product {productId} is already in user {userId}'s wishlist") { }
}
