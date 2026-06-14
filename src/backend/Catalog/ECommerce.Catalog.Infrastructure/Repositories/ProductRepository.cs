using System;
using ECommerce.Catalog.Domain.Aggregates.Product;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.Queries;
using ECommerce.Catalog.Domain.ValueObjects;
using ECommerce.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Infrastructure.Repositories;

public class ProductRepository(CatalogDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var slugResult = Slug.Create(slug);
        if (!slugResult.IsSuccess)
            return Task.FromResult<Product?>(null);

        var parsedSlug = slugResult.GetDataOrThrow();
        return db.Products.Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Slug == parsedSlug, cancellationToken);
    }

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default)
    {
        var skuResult = Sku.Create(sku);
        if (!skuResult.IsSuccess)
            return Task.FromResult(false);

        var parsedSku = skuResult.GetDataOrThrow();
        return db.Products.AnyAsync(p => p.Sku != null && p.Sku == parsedSku, cancellationToken);
    }

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        var slugResult = Slug.Create(slug);
        if (!slugResult.IsSuccess)
            return Task.FromResult(false);

        var parsedSlug = slugResult.GetDataOrThrow();
        return db.Products.AnyAsync(p => p.Slug == parsedSlug, cancellationToken);
    }

    public Task<bool> ExistsByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
        => db.Products.AnyAsync(p => p.CategoryId == categoryId && p.Status == ProductStatus.Active, cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        ProductQueryParams p,
        CancellationToken cancellationToken = default)
    {
        var query = db.Products
            .AsNoTracking()
            .Where(x => x.Status == ProductStatus.Active);

        if (p.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == p.CategoryId.Value);

        if (p.IsFeatured.HasValue)
            query = query.Where(x => x.IsFeatured == p.IsFeatured.Value);

        if (p.MinPrice.HasValue)
            query = query.Where(x => EF.Property<decimal>(x, "Price") >= p.MinPrice.Value);

        if (p.MaxPrice.HasValue)
            query = query.Where(x => EF.Property<decimal>(x, "Price") <= p.MaxPrice.Value);

        if (p.MinRating.HasValue)
            query = query.Where(x =>
                db.ProductRatings
                  .Where(r => r.ProductId == x.Id)
                  .Select(r => (decimal?)r.AverageRating)
                  .FirstOrDefault() >= p.MinRating.Value);

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var term = $"%{p.Search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(EF.Property<string>(x, "Name"), term) ||
                EF.Functions.ILike(EF.Property<string>(x, "Sku"),  term));
        }

        query = p.SortBy?.Trim().ToLowerInvariant() switch
        {
            "name"       => query.OrderBy(x => EF.Property<string>(x, "Name")),
            "price-asc"  => query.OrderBy(x => EF.Property<decimal>(x, "Price")),
            "price-desc" => query.OrderByDescending(x => EF.Property<decimal>(x, "Price")),
            "newest"     => query.OrderByDescending(x => x.CreatedAt),
            "rating"     => query.OrderByDescending(x =>
                                db.ProductRatings
                                  .Where(r => r.ProductId == x.Id)
                                  .Select(r => (decimal?)r.AverageRating)
                                  .FirstOrDefault() ?? 0m),
            _            => query.OrderBy(x => EF.Property<string>(x, "Name")),
        };

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((p.Page - 1) * p.PageSize)
            .Take(p.PageSize)
            .AsSplitQuery()
            .Include(x => x.Images)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<Product>> GetFeaturedAsync(int limit, CancellationToken cancellationToken = default)
        => await db.Products
               .AsNoTracking()
               .Where(p => p.IsFeatured && p.Status == ProductStatus.Active)
               .OrderByDescending(p => p.CreatedAt)
               .Take(limit)
               .Include(p => p.Images)
               .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetFeaturedPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = db.Products.AsNoTracking().Where(p => p.IsFeatured && p.Status == ProductStatus.Active);
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .Include(p => p.Images)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<Product>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default)
        => await db.Products
               .AsNoTracking()
               .Where(p => p.StockQuantity <= threshold && p.Status == ProductStatus.Active)
               .OrderBy(p => p.StockQuantity)
               .Include(p => p.Images)
               .ToListAsync(cancellationToken);

    public Task<int> GetActiveProductsCountAsync(CancellationToken cancellationToken = default)
        => db.Products.AsNoTracking().CountAsync(p => p.Status == ProductStatus.Active, cancellationToken);

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        db.Products.Add(product);
        return Task.CompletedTask;
    }
}
