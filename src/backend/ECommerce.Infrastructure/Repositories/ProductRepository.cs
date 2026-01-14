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
}
