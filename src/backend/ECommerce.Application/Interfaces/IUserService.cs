using ECommerce.Application.DTOs.Users;
using ECommerce.Core.Results;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for user profile operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves a user's profile by user ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User profile DTO.</returns>
    Task<Result<UserProfileDto>> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user's profile information.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="dto">Updated profile information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated user profile DTO.</returns>
    Task<Result<UserProfileDto>> UpdateUserProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user's preferences.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User preferences DTO.</returns>
    Task<Result<UserPreferencesDto>> GetUserPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user's preferences.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="dto">Updated preferences.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated user preferences DTO.</returns>
    Task<Result<UserPreferencesDto>> UpdateUserPreferencesAsync(Guid userId, UserPreferencesDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes a user's password.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="oldPassword">The old password.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task completed.</returns>
    Task<Result<Unit>> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default);
}
