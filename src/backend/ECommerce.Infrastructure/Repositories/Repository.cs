using System.Linq.Expressions;
using ECommerce.Core.Common;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation providing data access abstraction.
/// All async methods support CancellationToken for graceful cancellation.
/// </summary>
/// <typeparam name="T">The entity type that inherits from BaseEntity.</typeparam>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<T> DbSet;

    /// <summary>
    /// Initializes a new instance of the Repository class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public Repository(AppDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    #region Read Operations

    /// <summary>
    /// Gets a single entity by its ID asynchronously.
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(Guid id, bool trackChanges = true, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <summary>
    /// Gets all entities asynchronously.
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetAllAsync(bool trackChanges = true, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all entities as IQueryable for further filtering.
    /// </summary>
    public virtual IQueryable<T> FindAll(bool trackChanges = false)
    {
        return trackChanges ? DbSet : DbSet.AsNoTracking();
    }

    /// <summary>
    /// Gets entities matching a condition as IQueryable.
    /// </summary>
    public virtual IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false)
    {
        return trackChanges
            ? DbSet.Where(expression)
            : DbSet.Where(expression).AsNoTracking();
    }

    #endregion

    #region Write Operations

    /// <summary>
    /// Adds a new entity (synchronous - does not save to database).
    /// </summary>
    public virtual void Add(T entity)
    {
        DbSet.Add(entity);
    }

    /// <summary>
    /// Adds a new entity asynchronously (does not save to database).
    /// </summary>
    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Adds multiple entities (synchronous - does not save to database).
    /// </summary>
    public virtual void AddRange(IEnumerable<T> entities)
    {
        DbSet.AddRange(entities);
    }

    /// <summary>
    /// Adds multiple entities asynchronously (does not save to database).
    /// </summary>
    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(entities, cancellationToken);
    }

    /// <summary>
    /// Updates an entity (synchronous - does not save to database).
    /// </summary>
    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    /// <summary>
    /// Updates an entity asynchronously (does not save to database).
    /// </summary>
    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        Update(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Deletes an entity (synchronous - does not save to database).
    /// </summary>
    public virtual void Delete(T entity)
    {
        DbSet.Remove(entity);
    }

    /// <summary>
    /// Deletes an entity asynchronously (does not save to database).
    /// </summary>
    public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        Delete(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Deletes multiple entities (synchronous - does not save to database).
    /// </summary>
    public virtual void DeleteRange(IEnumerable<T> entities)
    {
        DbSet.RemoveRange(entities);
    }

    /// <summary>
    /// Deletes multiple entities asynchronously (does not save to database).
    /// </summary>
    public virtual Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        DeleteRange(entities);
        return Task.CompletedTask;
    }

    #endregion

    #region Utility

    /// <summary>
    /// Checks if an entity with the specified ID exists asynchronously.
    /// </summary>
    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(x => x.Id == id, cancellationToken);
    }

    /// <summary>
    /// Counts all entities asynchronously.
    /// </summary>
    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Counts entities matching a predicate asynchronously.
    /// </summary>
    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(predicate, cancellationToken);
    }

    #endregion
}
