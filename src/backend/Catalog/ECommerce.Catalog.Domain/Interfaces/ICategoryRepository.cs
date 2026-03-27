using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Catalog.Domain.Aggregates.Category;

namespace ECommerce.Catalog.Domain.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
    /// <summary>
    /// Gets categories in a paginated form.
    /// </summary>
    Task<(IReadOnlyList<Category> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task<bool> HasProductsAsync(Guid categoryId, CancellationToken ct = default);
    /// <summary>
    /// Gets top-level categories (ParentId == null) in a paginated form.
    /// </summary>
    Task<(IReadOnlyList<Category> Items, int TotalCount)> GetTopLevelPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Category category, CancellationToken ct = default);
    Task UpdateAsync(Category category, CancellationToken ct = default);
    Task DeleteAsync(Category category, CancellationToken ct = default);
}
