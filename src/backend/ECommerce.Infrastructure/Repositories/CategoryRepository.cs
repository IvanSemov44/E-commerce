using System.Threading;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Category entity providing data access operations.
/// </summary>
public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Retrieves a category by its slug identifier.
    /// </summary>
    public async Task<Category?> GetBySlugAsync(string slug, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Include(c => c.Parent)
            .Include(c => c.Children)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive, cancellationToken);
    }

    /// <summary>
    /// Retrieves all top-level categories with their children.
    /// </summary>
    public async Task<IEnumerable<Category>> GetTopLevelCategoriesAsync(bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Where(c => c.ParentId == null && c.IsActive)
            .Include(c => c.Children)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves a category with all its children by ID.
    /// </summary>
    public async Task<IEnumerable<Category>> GetCategoryWithChildrenAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        var category = await query
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return category?.Children ?? new List<Category>();
    }

    /// <summary>
    /// Checks if a slug is unique within the database.
    /// </summary>
    public async Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return !await DbSet.AnyAsync(c => c.Slug == slug && c.Id != excludeId, cancellationToken);
    }

    /// <summary>
    /// Gets the product count for a specific category.
    /// </summary>
    public async Task<int> GetProductCountAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await DbSet
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
        return category?.Products.Count ?? 0;
    }
}
