using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Product?> GetBySlugAsync(string slug)
    {
        return await DbSet
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId)
    {
        return await DbSet
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetFeaturedAsync(int count)
    {
        return await DbSet
            .Where(p => p.IsFeatured && p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetActiveProductsAsync(int skip, int take)
    {
        return await DbSet
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> GetActiveProductsCountAsync()
    {
        return await DbSet.CountAsync(p => p.IsActive);
    }

    public async Task UpdateStockAsync(Guid productId, int quantity)
    {
        var product = await DbSet.FirstOrDefaultAsync(p => p.Id == productId);
        if (product != null)
        {
            product.StockQuantity = quantity;
            product.UpdatedAt = DateTime.UtcNow;
            await SaveChangesAsync();
        }
    }

    public async Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeId = null)
    {
        var exists = await DbSet
            .AnyAsync(p => p.Slug == slug && (excludeId == null || p.Id != excludeId));
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
        string? sortBy = null)
    {
        // Start with base query - only active products
        var query = DbSet
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Reviews)
            .AsQueryable();

        // Apply category filter if provided
        if (categoryId.HasValue && categoryId.Value != Guid.Empty)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var searchTerm = searchQuery.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(searchTerm) ||
                (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                (p.Sku != null && p.Sku.ToLower().Contains(searchTerm)));
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
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = sortBy?.ToLower() switch
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
            .ToListAsync();

        return (items, totalCount);
    }
}
