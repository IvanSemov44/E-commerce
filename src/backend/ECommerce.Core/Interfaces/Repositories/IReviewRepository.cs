using System.Threading;
using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

/// <summary>
/// Review repository interface for specialized review data access operations.
/// All async methods support CancellationToken for graceful cancellation.
/// </summary>
public interface IReviewRepository : IRepository<Review>
{
    /// <summary>
    /// Gets reviews for a product asynchronously.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="onlyApproved">Whether to retrieve only approved reviews.</param>
    /// <param name="trackChanges">Whether to track changes for the entities.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Reviews for the product.</returns>
    Task<IEnumerable<Review>> GetByProductIdAsync(Guid productId, bool onlyApproved = true, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated reviews for a product asynchronously.
    /// FIX: Added pagination to prevent loading all reviews for popular products.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="skip">Number of reviews to skip.</param>
    /// <param name="take">Number of reviews to take.</param>
    /// <param name="onlyApproved">Whether to retrieve only approved reviews.</param>
    /// <param name="trackChanges">Whether to track changes for the entities.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Paginated reviews for the product.</returns>
    Task<IEnumerable<Review>> GetByProductIdAsync(Guid productId, int skip, int take, bool onlyApproved = true, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets reviews written by a user asynchronously.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="trackChanges">Whether to track changes for the entities.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Reviews by the user.</returns>
    Task<IEnumerable<Review>> GetByUserIdAsync(Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a review with all details asynchronously.
    /// </summary>
    /// <param name="id">The review ID.</param>
    /// <param name="trackChanges">Whether to track changes for the entity.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The review with details.</returns>
    Task<Review?> GetByIdWithDetailsAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of approved reviews for a product asynchronously.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The count of approved reviews.</returns>
    Task<int> GetApprovedReviewCountAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the average rating for a product asynchronously.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The average rating.</returns>
    Task<decimal> GetAverageRatingAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has already reviewed a product asynchronously.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="productId">The product ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if user has reviewed the product; otherwise false.</returns>
    Task<bool> UserHasReviewedAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets reviews pending approval asynchronously.
    /// </summary>
    /// <param name="trackChanges">Whether to track changes for the entities.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Reviews pending approval.</returns>
    Task<IEnumerable<Review>> GetPendingApprovalAsync(bool trackChanges = false, CancellationToken cancellationToken = default);
}
