using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Catalog.Domain.Aggregates.Product;
using ECommerce.Catalog.Domain.Queries;
using ECommerce.Catalog.Domain.ValueObjects;

namespace ECommerce.Catalog.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(Product Product, string CategoryName)?> GetByIdWithCategoryAsync(Guid id, CancellationToken ct = default);
    Task<(Product Product, string CategoryName)?> GetBySlugWithCategoryAsync(Slug slug, CancellationToken ct = default);
    Task<bool> SkuExistsAsync(Sku sku, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(Slug slug, CancellationToken ct = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(ProductQueryParams queryParams, CancellationToken ct = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetFeaturedPagedAsync(int page, int pageSize, CancellationToken ct = default);
    // TODO Phase 3: move to Inventory context once InventoryItem aggregate exists
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetLowStockPagedAsync(int threshold, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, (decimal AverageRating, int ReviewCount)>> GetRatingsByProductIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<int> GetActiveProductsCountAsync(CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
}
