using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Catalog.Domain.Aggregates.Product;

namespace ECommerce.Catalog.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task<bool> ExistsByCategoryAsync(Guid categoryId, CancellationToken ct = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        Guid? categoryId = null, string? search = null,
        decimal? minPrice = null, decimal? maxPrice = null,
        CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetFeaturedAsync(int limit, CancellationToken ct = default);
    // TODO Phase 3: move to Inventory context once InventoryItem aggregate exists
    Task<IReadOnlyList<Product>> GetLowStockAsync(int threshold, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task DeleteAsync(Product product, CancellationToken ct = default);
}
