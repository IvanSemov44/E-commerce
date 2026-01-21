using ECommerce.Application.DTOs.Wishlist;

namespace ECommerce.Application.Services;

public interface IWishlistService
{
    Task<WishlistDto> GetUserWishlistAsync(Guid userId);
    Task<WishlistDto> AddToWishlistAsync(Guid userId, Guid productId);
    Task<WishlistDto> RemoveFromWishlistAsync(Guid userId, Guid productId);
    Task<bool> IsProductInWishlistAsync(Guid userId, Guid productId);
    Task<WishlistDto> ClearWishlistAsync(Guid userId);
}
