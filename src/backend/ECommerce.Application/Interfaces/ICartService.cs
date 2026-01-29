using ECommerce.Application.DTOs.Cart;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for managing shopping cart operations.
/// </summary>
public interface ICartService
{
    Task<CartDto> GetOrCreateCartAsync(Guid? userId, string? sessionId);
    Task<CartDto> GetCartAsync(Guid userId);
    Task<CartDto> AddToCartAsync(Guid? userId, string? sessionId, Guid productId, int quantity);
    Task<CartDto> UpdateCartItemAsync(Guid? userId, string? sessionId, Guid cartItemId, int quantity);
    Task<CartDto> RemoveFromCartAsync(Guid? userId, string? sessionId, Guid cartItemId);
    Task<CartDto> ClearCartAsync(Guid? userId, string? sessionId);
    Task<CartDto> GetCartByIdAsync(Guid cartId);
    Task ValidateCartAsync(Guid cartId);
}
