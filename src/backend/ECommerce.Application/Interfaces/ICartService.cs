using ECommerce.Application.DTOs.Cart;
using ECommerce.Core.Results;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for managing shopping cart operations.
/// </summary>
public interface ICartService
{
    Task<Result<CartDto>> GetOrCreateCartAsync(Guid? userId, string? sessionId, CancellationToken cancellationToken = default);
    Task<Result<CartDto>> GetCartAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<CartDto>> AddToCartAsync(Guid? userId, string? sessionId, Guid productId, int quantity, CancellationToken cancellationToken = default);
    Task<Result<CartDto>> UpdateCartItemAsync(Guid? userId, string? sessionId, Guid cartItemId, int quantity, CancellationToken cancellationToken = default);
    Task<Result<CartDto>> RemoveFromCartAsync(Guid? userId, string? sessionId, Guid cartItemId, CancellationToken cancellationToken = default);
    Task<Result<CartDto>> ClearCartAsync(Guid? userId, string? sessionId, CancellationToken cancellationToken = default);
    Task<Result<CartDto>> GetCartByIdAsync(Guid cartId, CancellationToken cancellationToken = default);
    Task<Result<Unit>> ValidateCartAsync(Guid cartId, CancellationToken cancellationToken = default);
    Task<Result<Unit>> ValidateCartAsync(Guid cartId, Guid? userId, bool isAdmin, CancellationToken cancellationToken = default);
}
