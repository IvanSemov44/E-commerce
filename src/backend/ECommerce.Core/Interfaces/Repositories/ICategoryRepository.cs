using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

public interface ICategoryRepository : IRepository<Category>
{
    Task<Category?> GetBySlugAsync(string slug, bool trackChanges = false);
    Task<IEnumerable<Category>> GetTopLevelCategoriesAsync(bool trackChanges = false);
    Task<IEnumerable<Category>> GetCategoryWithChildrenAsync(Guid id, bool trackChanges = false);
    Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeId = null);
    Task<int> GetProductCountAsync(Guid categoryId);
}
