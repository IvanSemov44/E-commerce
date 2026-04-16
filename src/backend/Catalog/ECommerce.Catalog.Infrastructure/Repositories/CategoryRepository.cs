using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.Aggregates.Category;
using ECommerce.Catalog.Domain.ValueObjects;
using ECommerce.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Infrastructure.Repositories;

public class CategoryRepository(CatalogDbContext db) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var slugResult = Slug.Create(slug);
        if (!slugResult.IsSuccess)
            return Task.FromResult<Category?>(null);

        var parsedSlug = slugResult.GetDataOrThrow();
        return db.Categories.FirstOrDefaultAsync(c => c.Slug == parsedSlug, cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        => await db.Categories.AsNoTracking().Where(c => c.IsActive).ToListAsync(cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        var slugResult = Slug.Create(slug);
        if (!slugResult.IsSuccess)
            return Task.FromResult(false);

        var parsedSlug = slugResult.GetDataOrThrow();
        return db.Categories.AnyAsync(c => c.Slug == parsedSlug, cancellationToken);
    }

    public Task<bool> HasProductsAsync(Guid categoryId, CancellationToken cancellationToken = default)
        => db.Products.AnyAsync(p => p.CategoryId == categoryId, cancellationToken);

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await db.Categories.AddAsync(category, cancellationToken);
    }

    public async Task<(IReadOnlyList<Category> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = db.Categories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => EF.Property<string>(c, "Name"));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<Category> Items, int TotalCount)> GetTopLevelPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = db.Categories.AsNoTracking().Where(c => c.IsActive && c.ParentId == null).OrderBy(c => EF.Property<string>(c, "Name"));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }
}
