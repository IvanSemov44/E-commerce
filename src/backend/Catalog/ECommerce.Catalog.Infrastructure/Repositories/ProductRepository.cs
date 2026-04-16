using System;
using ECommerce.Catalog.Domain.Aggregates.Product;
using ECommerce.Catalog.Domain.Interfaces;
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
        int page, int pageSize,
        Guid? categoryId = null,
        string? search = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        decimal? minRating = null,
        bool? isFeatured = null,
        string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Products.AsNoTracking().Where(p => p.Status == ProductStatus.Active);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (isFeatured.HasValue)
            query = query.Where(p => p.IsFeatured == isFeatured.Value);

        string? searchTerm = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        if (minPrice.HasValue)
            query = query.Where(p => EF.Property<decimal>(p, "Price") >= minPrice.Value);
        if (maxPrice.HasValue)
            query = query.Where(p => EF.Property<decimal>(p, "Price") <= maxPrice.Value);

        if (minRating.HasValue)
        {
            var rating = minRating.Value;
            query = query.Where(p => (db.ProductRatings
                .Where(r => r.ProductId == p.Id)
                .Select(r => (decimal?)r.AverageRating)
                .FirstOrDefault() ?? 0m) >= rating);
        }

        if (searchTerm is not null)
        {
            var materializedItems = await query
                .Include(p => p.Images)
                .ToListAsync(cancellationToken);

            var filtered = materializedItems.Where(p =>
                p.Name.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (p.Sku is not null && p.Sku.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));

            filtered = sortBy?.Trim().ToLowerInvariant() switch
            {
                "name"       => filtered.OrderBy(p => p.Name.Value),
                "price-asc"  => filtered.OrderBy(p => p.Price.Amount),
                "price-desc" => filtered.OrderByDescending(p => p.Price.Amount),
                "rating"     => filtered.OrderByDescending(p => db.ProductRatings
                                    .AsNoTracking()
                                    .Where(r => r.ProductId == p.Id)
                                    .Select(r => (decimal?)r.AverageRating)
                                    .FirstOrDefault() ?? 0m),
                "newest"     => filtered.OrderByDescending(p => p.CreatedAt),
                _            => filtered.OrderBy(p => p.Name.Value),
            };

            var totalFiltered = filtered.Count();
            var pagedFiltered = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (pagedFiltered, totalFiltered);
        }

        query = sortBy?.Trim().ToLowerInvariant() switch
        {
            "name"       => query.OrderBy(p => EF.Property<string>(p, "Name")),
            "price-asc"  => query.OrderBy(p => EF.Property<decimal>(p, "Price")),
            "price-desc" => query.OrderByDescending(p => EF.Property<decimal>(p, "Price")),
            "rating"     => query.OrderByDescending(p => db.ProductRatings
                                .Where(r => r.ProductId == p.Id)
                                .Select(r => (decimal?)r.AverageRating)
                                .FirstOrDefault() ?? 0m),
            "newest"     => query.OrderByDescending(p => p.CreatedAt),
            _            => query.OrderBy(p => EF.Property<string>(p, "Name")),
        };

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.Images)
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
