using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySlugAsync(string slug);
    Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId);
    Task<IEnumerable<Product>> GetFeaturedAsync(int count);
    Task<IEnumerable<Product>> GetActiveProductsAsync(int skip, int take);
    Task<int> GetActiveProductsCountAsync();
    Task<(IEnumerable<Product> Items, int TotalCount)> GetProductsWithFiltersAsync(
        int skip,
        int take,
        Guid? categoryId = null,
        string? searchQuery = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        decimal? minRating = null,
        bool? isFeatured = null,
        string? sortBy = null);
    Task UpdateStockAsync(Guid productId, int quantity);
    Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeId = null);
}
