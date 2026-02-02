using ECommerce.Application.DTOs.Wishlist;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for managing user wishlists.
/// </summary>
public interface IWishlistService
{
    Task<WishlistDto> GetUserWishlistAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<WishlistDto> AddToWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<WishlistDto> RemoveFromWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<bool> IsProductInWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<WishlistDto> ClearWishlistAsync(Guid userId, CancellationToken cancellationToken = default);
}
