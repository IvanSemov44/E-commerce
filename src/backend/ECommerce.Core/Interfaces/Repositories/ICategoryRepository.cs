using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

public interface ICategoryRepository : IRepository<Category>
{
    Task<Category?> GetBySlugAsync(string slug);
    Task<IEnumerable<Category>> GetTopLevelCategoriesAsync();
    Task<IEnumerable<Category>> GetCategoryWithChildrenAsync(Guid id);
    Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeId = null);
    Task<int> GetProductCountAsync(Guid categoryId);
}
