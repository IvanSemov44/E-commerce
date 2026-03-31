using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Users;
using ECommerce.Identity.Application.Commands.ChangePassword;
using ECommerce.Identity.Application.Commands.UpdateProfile;
using ECommerce.Identity.Application.Commands.UpdateUserPreferences;
using ECommerce.Identity.Application.Queries.GetCurrentUser;
using ECommerce.Identity.Application.Queries.GetUserPreferences;
using ECommerce.API.ActionFilters;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for managing user profile operations.
/// Dispatches to Identity CQRS handlers via IMediator.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Profile")]
[Authorize]
public class ProfileController(IMediator mediator, ILogger<ProfileController> logger) : ControllerBase
{
    private Guid? GetUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }

    private IActionResult MapError(ECommerce.SharedKernel.Results.DomainError error) => error.Code switch
    {
        "INVALID_CREDENTIALS" or "TOKEN_INVALID" or "TOKEN_REVOKED"
            => Unauthorized(ApiResponse<object>.Failure(error.Message, error.Code)),
        "USER_NOT_FOUND"
            => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),
        "VALIDATION_FAILED"
            => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code)),
        _ => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code))
    };

    /// <summary>Retrieves the authenticated user's profile information.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ECommerce.Identity.Application.DTOs.UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        logger.LogInformation("Retrieving profile for user {UserId}", userId.Value);
        var result = await mediator.Send(new GetCurrentUserQuery(userId.Value), ct);
        if (!result.IsSuccess) return MapError(result.GetErrorOrThrow());

        return Ok(ApiResponse<ECommerce.Identity.Application.DTOs.UserProfileDto>.Ok(result.GetDataOrThrow(), "Profile retrieved successfully"));
    }

    /// <summary>Updates the authenticated user's profile information.</summary>
    [HttpPut]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<ECommerce.Identity.Application.DTOs.UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        logger.LogInformation("Updating profile for user {UserId}", userId.Value);
        var result = await mediator.Send(new UpdateProfileCommand(userId.Value, dto.FirstName, dto.LastName, dto.Phone), ct);
        if (!result.IsSuccess) return MapError(result.GetErrorOrThrow());

        return Ok(ApiResponse<ECommerce.Identity.Application.DTOs.UserProfileDto>.Ok(result.GetDataOrThrow(), "Profile updated successfully"));
    }

    /// <summary>Retrieves the authenticated user's preferences.</summary>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(ApiResponse<ECommerce.Identity.Application.DTOs.UserPreferencesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        logger.LogInformation("Retrieving preferences for user {UserId}", userId.Value);
        var result = await mediator.Send(new GetUserPreferencesQuery(userId.Value), ct);
        if (!result.IsSuccess) return MapError(result.GetErrorOrThrow());

        return Ok(ApiResponse<ECommerce.Identity.Application.DTOs.UserPreferencesDto>.Ok(result.GetDataOrThrow(), "Preferences retrieved successfully"));
    }

    /// <summary>Updates the authenticated user's preferences.</summary>
    [HttpPut("preferences")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<ECommerce.Identity.Application.DTOs.UserPreferencesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePreferences([FromBody] ECommerce.Application.DTOs.Users.UserPreferencesDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        logger.LogInformation("Updating preferences for user {UserId}", userId.Value);
        var result = await mediator.Send(new UpdateUserPreferencesCommand(
            userId.Value, dto.EmailNotifications, dto.SmsNotifications, dto.PushNotifications,
            dto.Language, dto.Currency, dto.NewsletterSubscribed), ct);
        if (!result.IsSuccess) return MapError(result.GetErrorOrThrow());

        return Ok(ApiResponse<ECommerce.Identity.Application.DTOs.UserPreferencesDto>.Ok(result.GetDataOrThrow(), "Preferences updated successfully"));
    }

    /// <summary>Changes the authenticated user's password.</summary>
    [HttpPost("change-password")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        logger.LogInformation("Changing password for user {UserId}", userId.Value);

        if (dto.NewPassword != dto.ConfirmPassword)
            return BadRequest(ApiResponse<object>.Failure("New password and confirmation do not match", "PASSWORD_MISMATCH"));

        var result = await mediator.Send(new ChangePasswordCommand(userId.Value, dto.OldPassword, dto.NewPassword), ct);
        if (!result.IsSuccess) return MapError(result.GetErrorOrThrow());

        return Ok(ApiResponse<object>.Ok(new object(), "Password changed successfully"));
    }
}
