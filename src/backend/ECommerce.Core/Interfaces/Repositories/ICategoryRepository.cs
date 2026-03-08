using System.Threading;
using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

/// <summary>
/// Category repository interface for specialized category data access operations.
/// All async methods support CancellationToken for graceful cancellation.
/// </summary>
public interface ICategoryRepository : IRepository<Category>
{
    /// <summary>
    /// Gets a category by its slug asynchronously.
    /// </summary>
    /// <param name="slug">The category slug.</param>
    /// <param name="trackChanges">Whether to track changes for the entity.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The category if found; otherwise null.</returns>
    Task<Category?> GetBySlugAsync(string slug, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all top-level categories (without parents) asynchronously.
    /// </summary>
    /// <param name="trackChanges">Whether to track changes for the entities.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Top-level categories.</returns>
    Task<IEnumerable<Category>> GetTopLevelCategoriesAsync(bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a category with all its children asynchronously.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="trackChanges">Whether to track changes for the entities.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The category with children.</returns>
    Task<IEnumerable<Category>> GetCategoryWithChildrenAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a category slug is unique asynchronously.
    /// </summary>
    /// <param name="slug">The slug to check.</param>
    /// <param name="excludeId">Optional category ID to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if slug is unique; otherwise false.</returns>
    Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the product count for a category asynchronously.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The count of products in the category.</returns>
    Task<int> GetProductCountAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets product counts for multiple categories in a single query (avoids N+1).
    /// </summary>
    /// <param name="categoryIds">The category IDs.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Dictionary of category ID to product count.</returns>
    Task<Dictionary<Guid, int>> GetProductCountsAsync(IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default);
}
