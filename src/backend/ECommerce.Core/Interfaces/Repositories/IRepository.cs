using System.Linq.Expressions;
using ECommerce.Core.Common;

namespace ECommerce.Core.Interfaces.Repositories;

/// <summary>
/// Generic repository interface providing data access abstraction.
/// All async methods support CancellationToken for graceful cancellation.
/// </summary>
/// <typeparam name="T">The entity type that inherits from BaseEntity.</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    #region Read Operations

    /// <summary>
    /// Gets a single entity by its ID asynchronously.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <param name="trackChanges">Whether to track changes for this entity.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The entity if found; otherwise null.</returns>
    Task<T?> GetByIdAsync(Guid id, bool trackChanges = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple entities by their IDs asynchronously (prevents N+1 queries).
    /// </summary>
    /// <param name="ids">The entity IDs to retrieve.</param>
    /// <param name="trackChanges">Whether to track changes for retrieved entities.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Entities matching the provided IDs.</returns>
    Task<IEnumerable<T>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities asynchronously.
    /// </summary>
    /// <param name="trackChanges">Whether to track changes for retrieved entities.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>All entities of type T.</returns>
    Task<IEnumerable<T>> GetAllAsync(bool trackChanges = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities as IQueryable for further filtering.
    /// </summary>
    /// <param name="trackChanges">Whether to track changes for the query.</param>
    /// <returns>IQueryable of all entities.</returns>
    IQueryable<T> FindAll(bool trackChanges = false);

    /// <summary>
    /// Gets entities matching a condition as IQueryable.
    /// </summary>
    /// <param name="expression">The filter expression.</param>
    /// <param name="trackChanges">Whether to track changes for the query.</param>
    /// <returns>IQueryable of matching entities.</returns>
    IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false);

    #endregion

    #region Write Operations

    /// <summary>
    /// Adds a new entity (synchronous - does not save to database).
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    void Add(T entity);

    /// <summary>
    /// Adds a new entity asynchronously (does not save to database).
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities (synchronous - does not save to database).
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    void AddRange(IEnumerable<T> entities);

    /// <summary>
    /// Adds multiple entities asynchronously (does not save to database).
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an entity (synchronous - does not save to database).
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    void Update(T entity);

    /// <summary>
    /// Updates an entity asynchronously (does not save to database).
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple entities (synchronous - does not save to database).
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    void UpdateRange(IEnumerable<T> entities);

    /// <summary>
    /// Updates multiple entities asynchronously (does not save to database).
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity asynchronously (does not save to database).
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity (synchronous - does not save to database).
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    void Delete(T entity);

    /// <summary>
    /// Deletes multiple entities (synchronous - does not save to database).
    /// </summary>
    /// <param name="entities">The entities to delete.</param>
    void DeleteRange(IEnumerable<T> entities);

    /// <summary>
    /// Deletes multiple entities asynchronously (does not save to database).
    /// </summary>
    /// <param name="entities">The entities to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    #endregion

    #region Utility

    /// <summary>
    /// Checks if an entity with the specified ID exists asynchronously.
    /// </summary>
    /// <param name="id">The entity ID to check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if entity exists; otherwise false.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts all entities asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The count of all entities.</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching a predicate asynchronously.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The count of matching entities.</returns>
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    #endregion
}
