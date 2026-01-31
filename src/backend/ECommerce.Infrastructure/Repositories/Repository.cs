using System.Linq.Expressions;
using ECommerce.Core.Common;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(AppDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    #region Read Operations

    public virtual async Task<T?> GetByIdAsync(Guid id, bool trackChanges = true)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query.FirstOrDefaultAsync(x => x.Id == id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(bool trackChanges = true)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query.ToListAsync();
    }

    public virtual IQueryable<T> FindAll(bool trackChanges = false)
    {
        return trackChanges ? DbSet : DbSet.AsNoTracking();
    }

    public virtual IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false)
    {
        return trackChanges
            ? DbSet.Where(expression)
            : DbSet.Where(expression).AsNoTracking();
    }

    #endregion

    #region Write Operations

    public virtual void Add(T entity)
    {
        DbSet.Add(entity);
    }

    public virtual async Task AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
    }

    public virtual void AddRange(IEnumerable<T> entities)
    {
        DbSet.AddRange(entities);
    }

    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public virtual Task UpdateAsync(T entity)
    {
        Update(entity);
        return Task.CompletedTask;
    }

    public virtual void Delete(T entity)
    {
        DbSet.Remove(entity);
    }

    public virtual Task DeleteAsync(T entity)
    {
        Delete(entity);
        return Task.CompletedTask;
    }

    public virtual void DeleteRange(IEnumerable<T> entities)
    {
        DbSet.RemoveRange(entities);
    }

    #endregion

    #region Utility

    public virtual async Task<bool> ExistsAsync(Guid id)
    {
        return await DbSet.AnyAsync(x => x.Id == id);
    }

    public virtual async Task<int> CountAsync()
    {
        return await DbSet.CountAsync();
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.CountAsync(predicate);
    }

    #endregion
}
