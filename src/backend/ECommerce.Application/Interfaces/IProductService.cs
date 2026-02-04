using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Products;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for product management operations.
/// All async methods support CancellationToken for graceful cancellation.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Gets paginated products with optional filters.
    /// </summary>
    Task<PaginatedResult<ProductDto>> GetProductsAsync(ProductQueryParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product by its slug (URL-friendly identifier).
    /// </summary>
    Task<ProductDetailDto> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product by its ID.
    /// </summary>
    Task<ProductDetailDto> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets featured products limited to the specified count.
    /// </summary>
    Task<List<ProductDto>> GetFeaturedProductsAsync(int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new product.
    /// </summary>
    Task<ProductDetailDto> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    Task<ProductDetailDto> UpdateProductAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a product.
    /// </summary>
    Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active products with pagination.
    /// </summary>
    Task<PaginatedResult<ProductDto>> GetAllProductsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches products by name, description, or SKU.
    /// </summary>
    Task<PaginatedResult<ProductDto>> SearchProductsAsync(string query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products in a specific category.
    /// </summary>
    Task<PaginatedResult<ProductDto>> GetProductsByCategoryAsync(Guid categoryId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products within a price range.
    /// </summary>
    Task<PaginatedResult<ProductDto>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products with low stock levels.
    /// </summary>
    Task<List<ProductDto>> GetLowStockProductsAsync(CancellationToken cancellationToken = default);
}
