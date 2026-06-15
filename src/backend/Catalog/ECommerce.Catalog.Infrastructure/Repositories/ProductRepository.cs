using System.Linq.Expressions;
using ECommerce.Catalog.Domain.Aggregates.Product;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.Queries;
using ECommerce.Catalog.Domain.ValueObjects;
using ECommerce.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Infrastructure.Repositories;

public class ProductRepository(CatalogDbContext db) : IProductRepository
{
    // No AsNoTracking — command handlers need change tracking to persist mutations.
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    // AsNoTracking — query paths only; mutations use GetByIdAsync above.
    public Task<(Product Product, string CategoryName)?> GetByIdWithCategoryAsync(Guid id, CancellationToken ct = default)
        => GetWithCategoryAsync(p => p.Id == id, ct);

    public Task<(Product Product, string CategoryName)?> GetBySlugWithCategoryAsync(Slug slug, CancellationToken ct = default)
        => GetWithCategoryAsync(p => p.Slug == slug, ct);

    private async Task<(Product Product, string CategoryName)?> GetWithCategoryAsync(
        Expression<Func<Product, bool>> predicate,
        CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking().Include(p => p.Images)
            .FirstOrDefaultAsync(predicate, ct);
        if (product is null) return null;

        var categoryName = await db.Categories.AsNoTracking()
            .Where(c => c.Id == product.CategoryId)
            .Select(c => c.Name.Value)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        return (product, categoryName);
    }

    public Task<bool> SkuExistsAsync(Sku sku, CancellationToken cancellationToken = default)
        => db.Products.AnyAsync(p => p.Sku != null && p.Sku == sku, cancellationToken);

    public Task<bool> SlugExistsAsync(Slug slug, CancellationToken cancellationToken = default)
        => db.Products.AnyAsync(p => p.Slug == slug, cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        ProductQueryParams p,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = BuildSqlFilteredQuery(p);
        var ordered   = ApplySorting(baseQuery, NormalizeSortBy(p.SortBy));

        bool needsInMemoryFilter = p.MinPrice.HasValue || p.MaxPrice.HasValue || !string.IsNullOrWhiteSpace(p.Search);

        if (!needsInMemoryFilter)
        {
            int total = await ordered.CountAsync(cancellationToken);
            var ids = await ordered
                .Skip((p.Page - 1) * p.PageSize)
                .Take(p.PageSize)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            return (await LoadByIdsAsync(ids, cancellationToken), total);
        }

        // EF Core cannot translate value-object member access (x.Price.Amount, x.Name.Value) in WHERE
        // clauses when using HasConversion with these value types. Materialize the SQL-filtered set
        // (status/category/featured/rating already applied), apply price/text in-memory, then load
        // full entities (with images) for just the final page.
        // TODO: migrate Money/ProductName to EF Core ComplexProperty to re-enable SQL filtering.
        IEnumerable<Product> filtered = await ordered.AsNoTracking().ToListAsync(cancellationToken);

        if (p.MinPrice.HasValue) filtered = filtered.Where(x => x.Price.Amount >= p.MinPrice.Value);
        if (p.MaxPrice.HasValue) filtered = filtered.Where(x => x.Price.Amount <= p.MaxPrice.Value);

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var term = p.Search.Trim();
            filtered = filtered.Where(x =>
                x.Name.Value.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (x.Sku?.Value.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var filteredList  = filtered.ToList();
        int filteredTotal = filteredList.Count;
        var pageIds       = filteredList
            .Skip((p.Page - 1) * p.PageSize)
            .Take(p.PageSize)
            .Select(x => x.Id)
            .ToList();

        return (await LoadByIdsAsync(pageIds, cancellationToken), filteredTotal);
    }

    // Applies only SQL-translatable filters (Active status, CategoryId, IsFeatured, MinRating).
    // Price range and text search cannot be translated through the Money/ProductName value converters
    // in the current EF Core configuration; those are applied in-memory after this query.
    private IQueryable<Product> BuildSqlFilteredQuery(ProductQueryParams p)
    {
        var query = db.Products
            .AsNoTracking()
            .Where(x => x.Status == ProductStatus.Active);

        if (p.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == p.CategoryId.Value);

        if (p.IsFeatured.HasValue)
            query = query.Where(x => x.IsFeatured == p.IsFeatured.Value);

        if (p.MinRating.HasValue)
        {
            // ProductRatings is a read model with PK=ProductId; .Any() generates a WHERE EXISTS with a PK seek
            query = query.Where(x =>
                db.ProductRatings.Any(r => r.ProductId == x.Id && r.AverageRating >= p.MinRating.Value));
        }

        return query;
    }

    private IQueryable<Product> ApplySorting(IQueryable<Product> query, string sortBy)
        => sortBy switch
        {
            // EF.Property with the store type (decimal/string) is used because value-object member access
            // (x.Price.Amount, x.Name.Value) is not translatable through these converters in EF Core.
            "price-asc"  => query.OrderBy(x => EF.Property<decimal>(x, "Price")).ThenBy(x => EF.Property<string>(x, "Name")).ThenBy(x => x.Id),
            "price-desc" => query.OrderByDescending(x => EF.Property<decimal>(x, "Price")).ThenBy(x => EF.Property<string>(x, "Name")).ThenBy(x => x.Id),
            "newest"     => query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id),
            // Scalar subquery — ProductRatings has PK=ProductId so each lookup is a single index seek
            "rating"     => query.OrderByDescending(x =>
                                (decimal?)db.ProductRatings
                                    .Where(r => r.ProductId == x.Id)
                                    .Select(r => r.AverageRating)
                                    .FirstOrDefault() ?? 0m)
                                .ThenBy(x => EF.Property<string>(x, "Name"))
                                .ThenBy(x => x.Id),
            _            => query.OrderBy(x => EF.Property<string>(x, "Name")).ThenBy(x => x.Id)
        };

    private async Task<IReadOnlyList<Product>> LoadByIdsAsync(List<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return [];

        var items = await db.Products
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .AsSplitQuery()
            .Include(x => x.Images)
            .ToListAsync(ct);

        // Restore sort order established by the paging query
        var byId = items.ToDictionary(x => x.Id);
        return ids.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
    }

    private static string NormalizeSortBy(string? sortBy)
        => sortBy?.Trim().ToLowerInvariant() ?? "name";

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetFeaturedPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = FeaturedBaseQuery();
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

    private IQueryable<Product> FeaturedBaseQuery()
        => db.Products.AsNoTracking().Where(p => p.IsFeatured && p.Status == ProductStatus.Active);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetLowStockPagedAsync(int threshold, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = db.Products
            .AsNoTracking()
            .Where(p => p.StockQuantity <= threshold && p.Status == ProductStatus.Active)
            .OrderBy(p => p.StockQuantity);

        int total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .Include(p => p.Images)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyDictionary<Guid, (decimal AverageRating, int ReviewCount)>> GetRatingsByProductIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
            return new Dictionary<Guid, (decimal, int)>();

        return await db.ProductRatings
            .AsNoTracking()
            .Where(r => idList.Contains(r.ProductId))
            .ToDictionaryAsync(
                r => r.ProductId,
                r => (r.AverageRating, r.ReviewCount),
                cancellationToken);
    }

    public Task<int> GetActiveProductsCountAsync(CancellationToken cancellationToken = default)
        => db.Products.AsNoTracking().CountAsync(p => p.Status == ProductStatus.Active, cancellationToken);

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        db.Products.Add(product);
        return Task.CompletedTask;
    }
}
