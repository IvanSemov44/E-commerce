using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Users;
using ECommerce.Application.Services;
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

    private Guid GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim?.Value == null)
            throw new UnauthorizedAccessException("User not authenticated");
        return Guid.Parse(userIdClaim.Value);
    }

    /// <summary>
    /// Retrieves the authenticated user's profile information.
    /// </summary>
    /// <returns>User profile details.</returns>
    /// <response code="200">Profile retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="404">User profile not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            var profile = await _userService.GetUserProfileAsync(userId);

            if (profile == null)
            {
                return NotFound(ApiResponse<UserProfileDto>.Error("User profile not found"));
            }

            return Ok(ApiResponse<UserProfileDto>.Ok(profile, "Profile retrieved successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(ApiResponse<UserProfileDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return StatusCode(500, ApiResponse<UserProfileDto>.Error("An error occurred while retrieving the profile"));
        }
    }

    /// <summary>
    /// Updates the authenticated user's profile information.
    /// </summary>
    /// <param name="updateProfileDto">Updated profile information.</param>
    /// <returns>Updated user profile.</returns>
    /// <response code="200">Profile updated successfully.</response>
    /// <response code="400">Invalid profile data.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="404">User profile not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile([FromBody] UpdateProfileDto updateProfileDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<UserProfileDto>.Error("Validation failed", errors));
            }

            var userId = GetAuthenticatedUserId();
            var profile = await _userService.UpdateUserProfileAsync(userId, updateProfileDto);

            _logger.LogInformation("Profile updated for user {UserId}", userId);
            return Ok(ApiResponse<UserProfileDto>.Ok(profile, "Profile updated successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(ApiResponse<UserProfileDto>.Error(ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Profile update failed: {Message}", ex.Message);
            return NotFound(ApiResponse<UserProfileDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return StatusCode(500, ApiResponse<UserProfileDto>.Error("An error occurred while updating the profile"));
        }
    }
}
