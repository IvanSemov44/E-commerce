using ECommerce.API.ActionFilters;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Users;
using ECommerce.Application.Interfaces;
using ECommerce.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for managing user profile operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IUserService userService, ICurrentUserService currentUser, ILogger<ProfileController> logger)
    {
        _userService = userService;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the authenticated user's profile information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's profile details.</returns>
    /// <response code="200">Profile retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">User profile not found.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("Retrieving profile for user {UserId}", userId);

        var result = await _userService.GetUserProfileAsync(userId, cancellationToken: cancellationToken);
        return result is Result<UserProfileDto>.Success success
            ? Ok(ApiResponse<UserProfileDto>.Ok(success.Data, "Profile retrieved successfully"))
            : result is Result<UserProfileDto>.Failure failure
                ? BadRequest(ApiResponse<UserProfileDto>.Failure(failure.Message, failure.Code))
                : BadRequest(ApiResponse<UserProfileDto>.Failure("An error occurred", "UNKNOWN_ERROR"));
    }

    /// <summary>
    /// Updates the authenticated user's profile information.
    /// </summary>
    /// <param name="updateProfileDto">The updated profile details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user profile.</returns>
    /// <response code="200">Profile updated successfully.</response>
    /// <response code="400">Invalid profile data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">User profile not found.</response>
    [HttpPut]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateProfileDto, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("Updating profile for user {UserId}", userId);

        var result = await _userService.UpdateUserProfileAsync(userId, updateProfileDto, cancellationToken: cancellationToken);
        return result is Result<UserProfileDto>.Success success
            ? Ok(ApiResponse<UserProfileDto>.Ok(success.Data, "Profile updated successfully"))
            : result is Result<UserProfileDto>.Failure failure
                ? BadRequest(ApiResponse<UserProfileDto>.Failure(failure.Message, failure.Code))
                : BadRequest(ApiResponse<UserProfileDto>.Failure("An error occurred", "UNKNOWN_ERROR"));
    }

    /// <summary>
    /// Retrieves the authenticated user's preferences.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's preferences.</returns>
    /// <response code="200">Preferences retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(ApiResponse<UserPreferencesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreferences(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("Retrieving preferences for user {UserId}", userId);

        var result = await _userService.GetUserPreferencesAsync(userId, cancellationToken: cancellationToken);
        
        if (result is Result<UserPreferencesDto>.Success success)
            return Ok(ApiResponse<UserPreferencesDto>.Ok(success.Data, "Preferences retrieved successfully"));
        
        if (result is Result<UserPreferencesDto>.Failure failure)
            return NotFound(ApiResponse<object>.Failure(failure.Message, failure.Code));
        
        return StatusCode(500, ApiResponse<object>.Failure("Unknown error occurred", "INTERNAL_ERROR"));
    }

    /// <summary>
    /// Updates the authenticated user's preferences.
    /// </summary>
    /// <param name="dto">The updated preferences.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user preferences.</returns>
    /// <response code="200">Preferences updated successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">User not found.</response>
    [HttpPut("preferences")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<UserPreferencesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UserPreferencesDto dto, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("Updating preferences for user {UserId}", userId);

        var result = await _userService.UpdateUserPreferencesAsync(userId, dto, cancellationToken: cancellationToken);
        
        if (result is Result<UserPreferencesDto>.Success success)
            return Ok(ApiResponse<UserPreferencesDto>.Ok(success.Data, "Preferences updated successfully"));
        
        if (result is Result<UserPreferencesDto>.Failure failure)
            return NotFound(ApiResponse<object>.Failure(failure.Message, failure.Code));
        
        return StatusCode(500, ApiResponse<object>.Failure("Unknown error occurred", "INTERNAL_ERROR"));
    }

    /// <summary>
    /// Changes the authenticated user's password.
    /// </summary>
    /// <param name="dto">The password change request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success message.</returns>
    /// <response code="200">Password changed successfully.</response>
    /// <response code="400">Invalid request or mismatched passwords.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">User not found.</response>
    [HttpPost("change-password")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("Changing password for user {UserId}", userId);

        // Validate that new passwords match
        if (dto.NewPassword != dto.ConfirmPassword)
        {
            return BadRequest(ApiResponse<object>.Failure("New password and confirmation do not match", "PASSWORD_MISMATCH"));
        }

        await _userService.ChangePasswordAsync(userId, dto.OldPassword, dto.NewPassword, cancellationToken: cancellationToken);
        return Ok(ApiResponse<object>.Ok(new object(), "Password changed successfully"));
    }
}

