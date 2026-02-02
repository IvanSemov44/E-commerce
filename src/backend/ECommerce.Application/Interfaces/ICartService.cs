using ECommerce.Application.DTOs.Cart;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for managing shopping cart operations.
/// </summary>
public interface ICartService
{
    Task<CartDto> GetOrCreateCartAsync(Guid? userId, string? sessionId, CancellationToken cancellationToken = default);
    Task<CartDto> GetCartAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CartDto> AddToCartAsync(Guid? userId, string? sessionId, Guid productId, int quantity, CancellationToken cancellationToken = default);
    Task<CartDto> UpdateCartItemAsync(Guid? userId, string? sessionId, Guid cartItemId, int quantity, CancellationToken cancellationToken = default);
    Task<CartDto> RemoveFromCartAsync(Guid? userId, string? sessionId, Guid cartItemId, CancellationToken cancellationToken = default);
    Task<CartDto> ClearCartAsync(Guid? userId, string? sessionId, CancellationToken cancellationToken = default);
    Task<CartDto> GetCartByIdAsync(Guid cartId, CancellationToken cancellationToken = default);
    Task ValidateCartAsync(Guid cartId, CancellationToken cancellationToken = default);
}
