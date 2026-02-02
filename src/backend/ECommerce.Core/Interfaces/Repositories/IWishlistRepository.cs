using System.Threading;
using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

/// <summary>
/// Wishlist repository interface for specialized wishlist data access operations.
/// All async methods support CancellationToken for graceful cancellation.
/// </summary>
public interface IWishlistRepository : IRepository<Wishlist>
{
    /// <summary>
    /// Gets a wishlist by user ID asynchronously.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="trackChanges">Whether to track changes for the entity.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The wishlist if found; otherwise null.</returns>
    Task<Wishlist?> GetByUserIdAsync(Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a wishlist with all items by user ID asynchronously.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="trackChanges">Whether to track changes for the entities.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The wishlist with items.</returns>
    Task<Wishlist?> GetByUserIdWithItemsAsync(Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a product is in a user's wishlist asynchronously.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="productId">The product ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if product is in wishlist; otherwise false.</returns>
    Task<bool> IsProductInWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the item count in a user's wishlist asynchronously.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The count of items in the wishlist.</returns>
    Task<int> GetWishlistItemCountAsync(Guid userId, CancellationToken cancellationToken = default);
}
