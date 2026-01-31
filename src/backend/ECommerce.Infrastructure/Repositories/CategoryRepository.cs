using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Category?> GetBySlugAsync(string slug, bool trackChanges = false)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Include(c => c.Parent)
            .Include(c => c.Children)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive);
    }

    public async Task<IEnumerable<Category>> GetTopLevelCategoriesAsync(bool trackChanges = false)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Where(c => c.ParentId == null && c.IsActive)
            .Include(c => c.Children)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Category>> GetCategoryWithChildrenAsync(Guid id, bool trackChanges = false)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        var category = await query
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id);
        return category?.Children ?? new List<Category>();
    }

    public async Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeId = null)
    {
        return !await DbSet.AnyAsync(c => c.Slug == slug && c.Id != excludeId);
    }

    public async Task<int> GetProductCountAsync(Guid categoryId)
    {
        var category = await DbSet
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == categoryId);
        return category?.Products.Count ?? 0;
    }
}
