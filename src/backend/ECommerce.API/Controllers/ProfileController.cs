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

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateProfileDto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Updating profile for user {UserId}", userId);

        var profile = await _userService.UpdateUserProfileAsync(userId, updateProfileDto, cancellationToken: cancellationToken);
        return Ok(ApiResponse<UserProfileDto>.Ok(profile, "Profile updated successfully"));
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
