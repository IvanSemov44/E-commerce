using System.Linq.Expressions;
using ECommerce.Core.Common;

namespace ECommerce.Core.Interfaces.Repositories;

public interface IRepository<T> where T : BaseEntity
{
    // Read operations with optional change tracking
    Task<T?> GetByIdAsync(Guid id, bool trackChanges = true);
    Task<IEnumerable<T>> GetAllAsync(bool trackChanges = true);
    IQueryable<T> FindAll(bool trackChanges = false);
    IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false);

    // Write operations (no SaveChanges - UnitOfWork handles that)
    void Add(T entity);
    Task AddAsync(T entity);
    void AddRange(IEnumerable<T> entities);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> entities);

    // Utility
    Task<bool> ExistsAsync(Guid id);
    Task<int> CountAsync();
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
}
