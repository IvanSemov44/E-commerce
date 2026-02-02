using System.Threading;
using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

/// <summary>
/// User repository interface for specialized user data access operations.
/// All async methods support CancellationToken for graceful cancellation.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Gets a user by email asynchronously.
    /// </summary>
    /// <param name="email">The user email.</param>
    /// <param name="trackChanges">Whether to track changes for the entity.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The user if found; otherwise null.</returns>
    Task<User?> GetByEmailAsync(string email, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user with all their addresses asynchronously.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="trackChanges">Whether to track changes for the entities.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The user with addresses.</returns>
    Task<User?> GetWithAddressesAsync(Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email exists in the system asynchronously.
    /// </summary>
    /// <param name="email">The email to check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if email exists; otherwise false.</returns>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by Google ID asynchronously.
    /// </summary>
    /// <param name="googleId">The Google ID.</param>
    /// <param name="trackChanges">Whether to track changes for the entity.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The user if found; otherwise null.</returns>
    Task<User?> GetByGoogleIdAsync(string googleId, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by Facebook ID asynchronously.
    /// </summary>
    /// <param name="facebookId">The Facebook ID.</param>
    /// <param name="trackChanges">Whether to track changes for the entity.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The user if found; otherwise null.</returns>
    Task<User?> GetByFacebookIdAsync(string facebookId, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of customers asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The count of customers.</returns>
    Task<int> GetCustomersCountAsync(CancellationToken cancellationToken = default);
}
