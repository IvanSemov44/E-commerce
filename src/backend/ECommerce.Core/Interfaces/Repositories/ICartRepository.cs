using System.Threading;
using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

/// <summary>
/// Cart repository interface for specialized cart data access operations.
/// All async methods support CancellationToken for graceful cancellation.
/// </summary>
public interface ICartRepository : IRepository<Cart>
{
    /// <summary>
    /// Gets a cart by user ID asynchronously.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="trackChanges">Whether to track changes for the entity.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The cart if found; otherwise null.</returns>
    Task<Cart?> GetByUserIdAsync(Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cart by session ID asynchronously.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="trackChanges">Whether to track changes for the entity.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The cart if found; otherwise null.</returns>
    Task<Cart?> GetBySessionIdAsync(string sessionId, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cart with all its items asynchronously.
    /// </summary>
    /// <param name="cartId">The cart ID.</param>
    /// <param name="trackChanges">Whether to track changes for the entities.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The cart with items.</returns>
    Task<Cart?> GetCartWithItemsAsync(Guid cartId, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a cart exists for a user asynchronously.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if cart exists; otherwise false.</returns>
    Task<bool> CartExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the total value of a cart asynchronously.
    /// </summary>
    /// <param name="cartId">The cart ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The total cart value.</returns>
    Task<decimal> CalculateTotalAsync(Guid cartId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the item count in a cart asynchronously.
    /// </summary>
    /// <param name="cartId">The cart ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The count of items in the cart.</returns>
    Task<int> GetCartItemCountAsync(Guid cartId, CancellationToken cancellationToken = default);
}
