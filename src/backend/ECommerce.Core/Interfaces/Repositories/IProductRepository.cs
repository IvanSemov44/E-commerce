using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySlugAsync(string slug, bool trackChanges = false);
    Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId, bool trackChanges = false);
    Task<IEnumerable<Product>> GetFeaturedAsync(int count, bool trackChanges = false);
    Task<IEnumerable<Product>> GetActiveProductsAsync(int skip, int take, bool trackChanges = false);
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
        string? sortBy = null,
        bool trackChanges = false);
    Task UpdateStockAsync(Guid productId, int quantity);
    Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeId = null);
}
