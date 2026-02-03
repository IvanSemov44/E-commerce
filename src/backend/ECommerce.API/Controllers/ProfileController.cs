using ECommerce.API.ActionFilters;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Users;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IUserService userService, ILogger<ProfileController> logger)
    {
        _userService = userService;
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
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Retrieving profile for user {UserId}", userId);

        var profile = await _userService.GetUserProfileAsync(userId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<UserProfileDto>.Ok(profile, "Profile retrieved successfully"));
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
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateProfileDto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Updating profile for user {UserId}", userId);

        var profile = await _userService.UpdateUserProfileAsync(userId, updateProfileDto, cancellationToken: cancellationToken);
        return Ok(ApiResponse<UserProfileDto>.Ok(profile, "Profile updated successfully"));
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
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreferences(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Retrieving preferences for user {UserId}", userId);

        var preferences = await _userService.GetUserPreferencesAsync(userId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<UserPreferencesDto>.Ok(preferences, "Preferences retrieved successfully"));
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
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UserPreferencesDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Updating preferences for user {UserId}", userId);

        var preferences = await _userService.UpdateUserPreferencesAsync(userId, dto, cancellationToken: cancellationToken);
        return Ok(ApiResponse<UserPreferencesDto>.Ok(preferences, "Preferences updated successfully"));
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim?.Value == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}
