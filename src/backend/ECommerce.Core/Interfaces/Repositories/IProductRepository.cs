using System.Threading;
using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

/// <summary>
/// Product repository interface for specialized product data access operations.
/// All async methods support CancellationToken for graceful cancellation.
/// </summary>
public interface IProductRepository : IRepository<Product>
{
    /// <summary>
    /// Gets a product by its slug asynchronously.
    /// </summary>
    /// <param name="slug">The product slug.</param>
    /// <param name="trackChanges">Whether to track changes for the entity.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The product if found; otherwise null.</returns>
    Task<Product?> GetBySlugAsync(string slug, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active products in a specific category asynchronously.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="trackChanges">Whether to track changes for the entities.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Products in the category.</returns>
    Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets featured products asynchronously.
    /// </summary>
    /// <param name="count">The number of featured products to retrieve.</param>
    /// <param name="trackChanges">Whether to track changes for the entities.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Featured products.</returns>
    Task<IEnumerable<Product>> GetFeaturedAsync(int count, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets featured products with pagination asynchronously.
    /// </summary>
    /// <param name="skip">Number of products to skip.</param>
    /// <param name="count">The number of featured products to retrieve.</param>
    /// <param name="trackChanges">Whether to track changes for the entities.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Featured products.</returns>
    Task<IEnumerable<Product>> GetFeaturedAsync(int skip, int count, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active products with pagination asynchronously.
    /// </summary>
    /// <param name="skip">Number of products to skip.</param>
    /// <param name="take">Number of products to take.</param>
    /// <param name="trackChanges">Whether to track changes for the entities.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Paginated active products.</returns>
    Task<IEnumerable<Product>> GetActiveProductsAsync(int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of all active products asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The count of active products.</returns>
    Task<int> GetActiveProductsCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of featured products asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The count of featured products.</returns>
    Task<int> GetFeaturedProductsCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products with applied filters asynchronously.
    /// </summary>
    /// <param name="skip">Number of products to skip.</param>
    /// <param name="take">Number of products to take.</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="searchQuery">Optional search term.</param>
    /// <param name="minPrice">Optional minimum price filter.</param>
    /// <param name="maxPrice">Optional maximum price filter.</param>
    /// <param name="minRating">Optional minimum rating filter.</param>
    /// <param name="isFeatured">Optional featured filter.</param>
    /// <param name="sortBy">Optional sort field.</param>
    /// <param name="trackChanges">Whether to track changes for the entities.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Tuple of filtered products and total count.</returns>
    Task<(IEnumerable<Product> Items, int TotalCount)> GetProductsWithFiltersAsync(
        int skip,
        int take,
        Guid? categoryId = null,
        string? searchQuery = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        decimal? minRating = null,
        bool? isFeatured = null,
        string? sortBy = null,
        bool trackChanges = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the stock quantity for a product asynchronously.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="quantity">The quantity change (can be positive or negative).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task UpdateStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a product slug is unique asynchronously.
    /// </summary>
    /// <param name="slug">The slug to check.</param>
    /// <param name="excludeId">Optional product ID to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if slug is unique; otherwise false.</returns>
    Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically reduces stock quantity for a product using optimistic concurrency.
    /// This method uses raw SQL to ensure atomic stock reduction and prevent race conditions.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="quantity">The quantity to reduce (must be positive).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if stock was successfully reduced; false if insufficient stock available.</returns>
    Task<bool> TryReduceStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products with stock less than or equal to the provided threshold.
    /// </summary>
    Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold, bool trackChanges = false, CancellationToken cancellationToken = default);
}
