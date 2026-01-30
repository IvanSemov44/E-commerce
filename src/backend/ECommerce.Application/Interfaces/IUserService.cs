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
    /// <returns>User profile DTO.</returns>
    Task<UserProfileDto> GetUserProfileAsync(Guid userId);

    /// <summary>
    /// Updates a user's profile information.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="dto">Updated profile information.</param>
    /// <returns>Updated user profile DTO.</returns>
    Task<UserProfileDto> UpdateUserProfileAsync(Guid userId, UpdateProfileDto dto);
}
