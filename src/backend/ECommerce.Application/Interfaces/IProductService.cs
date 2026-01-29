using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Products;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for product management operations.
/// </summary>
public interface IProductService
{
    Task<PaginatedResult<ProductDto>> GetProductsAsync(
        int page = 1,
        int pageSize = 20,
        Guid? categoryId = null,
        string? searchQuery = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        decimal? minRating = null,
        bool? isFeatured = null,
        string? sortBy = null);

    Task<ProductDetailDto> GetProductBySlugAsync(string slug);
    Task<ProductDetailDto> GetProductByIdAsync(Guid id);
    Task<List<ProductDto>> GetFeaturedProductsAsync(int count = 10);
    Task<ProductDetailDto> CreateProductAsync(CreateProductDto dto);
    Task<ProductDetailDto> UpdateProductAsync(Guid id, UpdateProductDto dto);
    Task DeleteProductAsync(Guid id);
}
