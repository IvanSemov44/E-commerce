using System.Reflection;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.Aggregates.Product;
using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using CoreProduct = ECommerce.Core.Entities.Product;
using CoreProductImage = ECommerce.Core.Entities.ProductImage;

namespace ECommerce.Catalog.Infrastructure.Repositories;

public class ProductRepository(CatalogDbContext _db) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var core = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (core is null) return null;
        return MapToDomain(core);
    }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var core = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
        if (core is null) return null;
        return MapToDomain(core);
    }

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default)
        => _db.Products.AnyAsync(p => p.Sku == sku, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
        => _db.Products.AnyAsync(p => p.Slug == slug, cancellationToken);

    public Task<bool> ExistsByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
        => _db.Products.AnyAsync(p => p.CategoryId == categoryId && p.IsActive, cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        Guid? categoryId = null,
        string? search = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        decimal? minRating = null, bool? isFeatured = null, string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Products.AsNoTracking().Where(p => p.IsActive);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (isFeatured.HasValue)
            query = query.Where(p => p.IsFeatured == isFeatured.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            if (_db.Database.ProviderName?.Contains("Npgsql") == true)
                query = query.Where(p => EF.Functions.ILike(p.Name, $"%{s}%") || EF.Functions.ILike(p.Sku ?? string.Empty, $"%{s}%"));
            else
                query = query.Where(p => p.Name.ToLower().Contains(s) || (p.Sku != null && p.Sku.ToLower().Contains(s)));
        }

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        if (minRating.HasValue)
        {
            // Filter by average rating (scalar subquery)
            var rating = (double)minRating.Value;
            query = query.Where(p => _db.ProductRatings.Where(r => r.ProductId == p.Id).Average(r => (double)r.Rating) >= rating);
        }

        // Sorting
        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            switch (sortBy.Trim().ToLowerInvariant())
            {
                case "name":
                    query = query.OrderBy(p => p.Name);
                    break;
                case "price-asc":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "price-desc":
                    query = query.OrderByDescending(p => p.Price);
                    break;
                case "rating":
                    query = query.OrderByDescending(p => _db.ProductRatings.Where(r => r.ProductId == p.Id).Average(r => (double)r.Rating));
                    break;
                case "newest":
                    query = query.OrderByDescending(p => p.CreatedAt);
                    break;
                default:
                    query = query.OrderBy(p => p.Name);
                    break;
            }
        }
        else
        {
            query = query.OrderBy(p => p.Name);
        }

        var total = await query.CountAsync(cancellationToken);

        var coreItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.Images)
            .ToListAsync(cancellationToken);

        var items = coreItems.Select(MapToDomain).ToList();
        return (items, total);
    }

    public async Task<IReadOnlyList<Product>> GetFeaturedAsync(int limit, CancellationToken cancellationToken = default)
    {
        var cores = await _db.Products
            .AsNoTracking()
            .Where(p => p.IsFeatured && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .Include(p => p.Images)
            .ToListAsync(cancellationToken);
        return cores.Select(MapToDomain).ToList();
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetFeaturedPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.Products.AsNoTracking().Where(p => p.IsFeatured && p.IsActive);

        var total = await query.CountAsync(cancellationToken);

        var coreItems = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.Images)
            .ToListAsync(cancellationToken);

        var items = coreItems.Select(MapToDomain).ToList();
        return (items, total);
    }

    public async Task<IReadOnlyList<Product>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default)
    {
        var cores = await _db.Products
            .AsNoTracking()
            .Where(p => p.StockQuantity <= threshold && p.IsActive)
            .Include(p => p.Images)
            .ToListAsync(cancellationToken);
        return cores.Select(MapToDomain).ToList();
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        var core = MapToCore(product);
        await _db.Products.AddAsync(core, cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == product.Id, cancellationToken);
        if (existing is null)
        {
            await AddAsync(product, cancellationToken);
            return;
        }

        existing.Name = product.Name.Value;
        existing.Slug = product.Slug.Value;
        existing.Price = product.Price.Amount;
        existing.CompareAtPrice = product.CompareAtPrice?.Amount;
        existing.Sku = product.Sku?.Value;
        existing.Description = product.Description;
        existing.IsFeatured = product.IsFeatured;
        existing.IsActive = product.Status == ProductStatus.Active;
        existing.CategoryId = product.CategoryId;

        existing.Images.Clear();
        foreach (var img in product.Images)
        {
            existing.Images.Add(new CoreProductImage { Id = img.Id, ProductId = existing.Id, Url = img.Url, AltText = img.AltText, IsPrimary = img.IsPrimary, SortOrder = img.DisplayOrder });
        }

        _db.Products.Update(existing);
    }

    public async Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Products.FirstOrDefaultAsync(p => p.Id == product.Id, cancellationToken);
        if (existing is null) return;
        _db.Products.Remove(existing);
    }

    private static Product MapToDomain(CoreProduct core)
    {
        var prodType = typeof(Product);
        var ctor = prodType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)!;
        var domain = (Product)ctor.Invoke(null);

        var name = ProductName.Create(core.Name).GetDataOrThrow();
        var slug = Slug.Create(core.Slug).GetDataOrThrow();
        var price = Money.Create(core.Price, "USD").GetDataOrThrow();
        Money? compare = null;
        if (core.CompareAtPrice.HasValue)
            compare = Money.Create(core.CompareAtPrice.Value, "USD").GetDataOrThrow();
        var sku = string.IsNullOrWhiteSpace(core.Sku) ? null : Sku.Create(core.Sku).GetDataOrThrow();

        prodType.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, name);
        prodType.GetProperty("Slug", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, slug);
        prodType.GetProperty("Price", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, price);
        prodType.GetProperty("CompareAtPrice", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, compare);
        prodType.GetProperty("Sku", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, sku);
        prodType.GetProperty("Description", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, core.Description);
        prodType.GetProperty("Status", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, core.IsActive ? ProductStatus.Active : ProductStatus.Inactive);
        prodType.GetProperty("IsFeatured", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, core.IsFeatured);
        prodType.GetProperty("StockQuantity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, core.StockQuantity);
        prodType.GetProperty("IsDeleted", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, false);
        prodType.GetProperty("CategoryId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, core.CategoryId ?? Guid.Empty);

        prodType.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, core.Id);
        prodType.GetProperty("CreatedAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, core.CreatedAt);
        prodType.GetProperty("UpdatedAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, core.UpdatedAt);

        var imagesField = prodType.GetField("_images", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var list = (System.Collections.IList)Activator.CreateInstance<List<ProductImage>>();
        var imgCtor = typeof(ProductImage).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(Guid), typeof(Guid), typeof(string), typeof(string), typeof(bool), typeof(int)], null);
        if (imgCtor is not null)
        {
            foreach (var ci in core.Images)
            {
                var img = (ProductImage)imgCtor.Invoke([ci.Id, ci.ProductId, ci.Url, ci.AltText, ci.IsPrimary, ci.SortOrder]);
                list.Add(img);
            }
        }
        imagesField.SetValue(domain, list);

        return domain;
    }

    private static CoreProduct MapToCore(Product domain)
    {
        var core = new CoreProduct
        {
            Id = domain.Id,
            Name = domain.Name.Value,
            Slug = domain.Slug.Value,
            Price = domain.Price.Amount,
            CompareAtPrice = domain.CompareAtPrice?.Amount,
            Sku = domain.Sku?.Value,
            Description = domain.Description,
            IsFeatured = domain.IsFeatured,
            StockQuantity = domain.StockQuantity,
            IsActive = domain.Status == ProductStatus.Active,
            CategoryId = domain.CategoryId,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt
        };

        core.Images = new List<CoreProductImage>();
        foreach (var img in domain.Images)
        {
            core.Images.Add(new CoreProductImage { Id = img.Id, ProductId = core.Id, Url = img.Url, AltText = img.AltText, IsPrimary = img.IsPrimary, SortOrder = img.DisplayOrder });
        }

        return core;
    }
}
