using System.Reflection;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.Aggregates.Category;
using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using CoreCategory = ECommerce.SharedKernel.Entities.Category;

namespace ECommerce.Catalog.Infrastructure.Repositories;

public class CategoryRepository(CatalogDbContext _db) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var core = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (core is null) return null;
        return MapToDomain(core);
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var core = await _db.Categories.FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);
        if (core is null) return null;
        return MapToDomain(core);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cores = await _db.Categories.AsNoTracking().Where(c => c.IsActive).ToListAsync(cancellationToken);
        return cores.Select(MapToDomain).ToList();
    }

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
        => _db.Categories.AnyAsync(c => c.Slug == slug, cancellationToken);

    public Task<bool> HasProductsAsync(Guid categoryId, CancellationToken cancellationToken = default)
        => _db.Products.AnyAsync(p => p.CategoryId == categoryId, cancellationToken);

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        var core = MapToCore(category);
        await _db.Categories.AddAsync(core, cancellationToken);
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Categories.FirstOrDefaultAsync(c => c.Id == category.Id, cancellationToken);
        if (existing is null)
        {
            await AddAsync(category, cancellationToken);
            return;
        }

        existing.Name = category.Name.Value;
        existing.Slug = category.Slug.Value;
        existing.IsActive = category.IsActive;
        existing.ParentId = category.ParentId;

        _db.Categories.Update(existing);
    }

    public async Task DeleteAsync(Category category, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Categories.FirstOrDefaultAsync(c => c.Id == category.Id, cancellationToken);
        if (existing is null) return;
        _db.Categories.Remove(existing);
    }

    public async Task<(IReadOnlyList<Category> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _db.Categories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name);
        var total = await query.CountAsync(cancellationToken);
        var cores = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var items = cores.Select(MapToDomain).ToList();
        return (items, total);
    }

    public async Task<(IReadOnlyList<Category> Items, int TotalCount)> GetTopLevelPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _db.Categories.AsNoTracking().Where(c => c.IsActive && c.ParentId == null).OrderBy(c => c.Name);
        var total = await query.CountAsync(cancellationToken);
        var cores = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var items = cores.Select(MapToDomain).ToList();
        return (items, total);
    }

    private static Category MapToDomain(CoreCategory core)
    {
        var ctor = typeof(Category).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)!;
        var domain = (Category)ctor.Invoke(null);
        var catType = typeof(Category);
        catType.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, core.Id);
        catType.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, CategoryName.Create(core.Name).GetDataOrThrow());
        catType.GetProperty("Slug", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, Slug.Create(core.Slug).GetDataOrThrow());
        catType.GetProperty("IsActive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, core.IsActive);
        catType.GetProperty("ParentId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(domain, core.ParentId);
        catType.GetProperty("CreatedAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, core.CreatedAt);
        catType.GetProperty("UpdatedAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(domain, core.UpdatedAt);
        return domain;
    }

    private static CoreCategory MapToCore(Category domain)
    {
        return new CoreCategory
        {
            Id = domain.Id,
            Name = domain.Name.Value,
            Slug = domain.Slug.Value,
            IsActive = domain.IsActive,
            ParentId = domain.ParentId,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt
        };
    }
}
