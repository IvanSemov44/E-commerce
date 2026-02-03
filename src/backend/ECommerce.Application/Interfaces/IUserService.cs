using ECommerce.Application.DTOs.Users;

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
    Task<UserProfileDto> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user's profile information.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="dto">Updated profile information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated user profile DTO.</returns>
    Task<UserProfileDto> UpdateUserProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user's preferences.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User preferences DTO.</returns>
    Task<UserPreferencesDto> GetUserPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user's preferences.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="dto">Updated preferences.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated user preferences DTO.</returns>
    Task<UserPreferencesDto> UpdateUserPreferencesAsync(Guid userId, UserPreferencesDto dto, CancellationToken cancellationToken = default);
}
