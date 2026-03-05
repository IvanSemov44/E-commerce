using ECommerce.Application.DTOs.Wishlist;
using ECommerce.Core.Results;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for managing user wishlists.
/// </summary>
public interface IWishlistService
{
    Task<Result<WishlistDto>> GetUserWishlistAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<WishlistDto>> AddToWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<Result<WishlistDto>> RemoveFromWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<bool> IsProductInWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<Result<WishlistDto>> ClearWishlistAsync(Guid userId, CancellationToken cancellationToken = default);
}
