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
    public Task<Category?> GetBySlugAsync(string slug, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return query
            .Include(c => c.Parent)
            .Include(c => c.Children)
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
    /// Gets the product count for a specific category using SQL COUNT (efficient).
    /// </summary>
    public Task<int> GetProductCountAsync(Guid categoryId, CancellationToken cancellationToken = default)
        => Context.Products
            .CountAsync(p => p.CategoryId == categoryId && p.IsActive, cancellationToken);

    /// <summary>
    /// Gets product counts for multiple categories in a single query (avoids N+1).
    /// </summary>
    public async Task<Dictionary<Guid, int>> GetProductCountsAsync(IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default)
    {
        var ids = categoryIds.ToList();
        if (!ids.Any())
            return new Dictionary<Guid, int>();

        // Single query to get all counts - handle nullable CategoryId
        var counts = await Context.Products
            .Where(p => p.IsActive && p.CategoryId.HasValue && ids.Contains(p.CategoryId.Value))
            .GroupBy(p => p.CategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // Build dictionary with all requested IDs (default to 0 if not found)
        var countDict = counts.ToDictionary(c => c.CategoryId, c => c.Count);
        return ids.ToDictionary(
            id => id,
            id => countDict.TryGetValue(id, out var count) ? count : 0
        );
    }
}
