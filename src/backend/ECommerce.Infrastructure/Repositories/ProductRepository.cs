using System.Threading;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

/// <summary>
/// Product repository implementation for product data access operations.
/// Implements IProductRepository with full CancellationToken support.
/// </summary>
public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context)
    {
    }

    public override Task<Product?> GetByIdAsync(Guid id, bool trackChanges = true, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return query
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Product?> GetBySlugAsync(string slug, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();

        // Reviews are fetched separately via ReviewRepository to avoid loading all reviews for popular products
        return query
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive, cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .ToListAsync(cancellationToken);
    }

    public Task<IEnumerable<Product>> GetFeaturedAsync(int count, bool trackChanges = false, CancellationToken cancellationToken = default)
        => GetFeaturedInternalAsync(skip: 0, count: count, trackChanges, cancellationToken);

    /// <summary>
    /// Gets featured products with pagination support.
    /// </summary>
    public Task<IEnumerable<Product>> GetFeaturedAsync(int skip, int count, bool trackChanges = false, CancellationToken cancellationToken = default)
        => GetFeaturedInternalAsync(skip, count, trackChanges, cancellationToken);

    private async Task<IEnumerable<Product>> GetFeaturedInternalAsync(int skip, int count, bool trackChanges, CancellationToken cancellationToken)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Where(p => p.IsFeatured && p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetActiveProductsAsync(int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetActiveProductsCountAsync(CancellationToken cancellationToken = default)
        => DbSet.CountAsync(p => p.IsActive, cancellationToken);

    /// <summary>
    /// Gets the count of featured products.
    /// </summary>
    public Task<int> GetFeaturedProductsCountAsync(CancellationToken cancellationToken = default)
        => DbSet.CountAsync(p => p.IsFeatured && p.IsActive, cancellationToken);

    public async Task UpdateStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        var product = await DbSet.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product != null)
        {
            product.StockQuantity = quantity;
            product.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var exists = await DbSet
            .AnyAsync(p => p.Slug == slug && (excludeId == null || p.Id != excludeId), cancellationToken);
        return !exists;
    }

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetProductsWithFiltersAsync(
        int skip,
        int take,
        Guid? categoryId = null,
        string? searchQuery = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        decimal? minRating = null,
        bool? isFeatured = null,
        string? sortBy = null,
        bool trackChanges = false,
        CancellationToken cancellationToken = default)
    {
        // Start with base query - only active products
        var baseQuery = trackChanges ? DbSet : DbSet.AsNoTracking();
        var query = baseQuery
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .AsQueryable();

        // Apply category filter if provided
        if (categoryId.HasValue && categoryId.Value != Guid.Empty)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var searchPattern = $"%{searchQuery}%";
            query = query.Where(p =>
                EF.Functions.Like(p.Name, searchPattern) ||
                (p.Description != null && EF.Functions.Like(p.Description, searchPattern)) ||
                (p.Sku != null && EF.Functions.Like(p.Sku, searchPattern)));
        }

        // Apply price range filters
        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        // Apply rating filter (only approved reviews)
        if (minRating.HasValue)
        {
            query = query.Where(p =>
                p.Reviews.Any(r => r.IsApproved) &&
                p.Reviews.Where(r => r.IsApproved).Average(r => r.Rating) >= (double)minRating.Value);
        }

        // Apply featured filter
        if (isFeatured.HasValue)
        {
            query = query.Where(p => p.IsFeatured == isFeatured.Value);
        }

        // Get total count AFTER filters applied
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = sortBy?.ToLowerInvariant() switch
        {
            "name" => query.OrderBy(p => p.Name),
            "price-asc" => query.OrderBy(p => p.Price),
            "price-desc" => query.OrderByDescending(p => p.Price),
            "rating" => query.OrderByDescending(p =>
                p.Reviews.Any(r => r.IsApproved)
                    ? p.Reviews.Where(r => r.IsApproved).Average(r => r.Rating)
                    : 0),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt) // Default: newest first
        };

        // Apply pagination
        var items = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <summary>
    /// Gets multiple products by their IDs in a single batch query.
    /// Overrides base implementation to eagerly load Category and Images navigation properties.
    /// Prevents N+1 query problems when loading multiple products.
    /// </summary>
    public override async Task<IEnumerable<Product>> GetByIdsAsync(IEnumerable<Guid> productIds, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Where(p => productIds.Contains(p.Id))
            .Include(p => p.Category)
            .Include(p => p.Images)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Atomically reduces stock quantity for a product using optimistic concurrency.
    /// Uses raw SQL to ensure atomic stock reduction and prevent race conditions.
    /// </summary>
    public async Task<bool> TryReduceStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        var affectedRows = await Context.Database.ExecuteSqlRawAsync(
            @"UPDATE Products
              SET StockQuantity = StockQuantity - {0}, UpdatedAt = {1}
              WHERE Id = {2} AND StockQuantity >= {0}",
            quantity, DateTime.UtcNow, productId, cancellationToken);

        return affectedRows > 0;
    }
}
