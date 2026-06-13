using ECommerce.API.ActionFilters;
using ECommerce.API.Common.Extensions;
using ECommerce.Contracts.DTOs.Common;
using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Commands.ChangePassword;
using ECommerce.Identity.Application.Commands.DeleteAddress;
using ECommerce.Identity.Application.Commands.UpdateProfile;
using ECommerce.Identity.Application.Commands.UpdateUserPreferences;
using ECommerce.Identity.Application.Queries.GetCurrentUser;
using ECommerce.Identity.Application.Queries.GetUserPreferences;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Features.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Profile")]
[Authorize]
public class ProfileController(IMediator mediator, ICurrentUserService currentUser, ILogger<ProfileController> logger) : ControllerBase
{
    private IActionResult MapError(DomainError error) => error.Code switch
    {
        "INVALID_CREDENTIALS" or "TOKEN_INVALID" or "TOKEN_REVOKED"
            => Unauthorized(ApiResponse<object>.Failure(error.Message, error.Code)),
        "USER_NOT_FOUND"
            => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),
        "VALIDATION_FAILED"
            => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code)),
        _ => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code))
    };

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        logger.LogInformation("Retrieving profile for user {UserId}", userId.Value);
        var result = await mediator.Send(new GetCurrentUserQuery(userId.Value), ct);
        return result.ToActionResult(
            data => Ok(ApiResponse<UserProfileDto>.Ok(data, "Profile retrieved successfully")),
            MapError);
    }

    [HttpPut]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken ct)
    {
        var userId = currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        logger.LogInformation("Updating profile for user {UserId}", userId.Value);
        var result = await mediator.Send(new UpdateProfileCommand(userId.Value, dto.FirstName, dto.LastName, dto.Phone), ct);
        return result.ToActionResult(
            data => Ok(ApiResponse<UserProfileDto>.Ok(data, "Profile updated successfully")),
            MapError);
    }

    [HttpGet("preferences")]
    [ProducesResponseType(typeof(ApiResponse<UserPreferencesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var userId = currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        logger.LogInformation("Retrieving preferences for user {UserId}", userId.Value);
        var result = await mediator.Send(new GetUserPreferencesQuery(userId.Value), ct);
        return result.ToActionResult(
            data => Ok(ApiResponse<UserPreferencesDto>.Ok(data, "Preferences retrieved successfully")),
            MapError);
    }

    [HttpPut("preferences")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<UserPreferencesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UserPreferencesDto dto, CancellationToken ct)
    {
        var userId = currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        logger.LogInformation("Updating preferences for user {UserId}", userId.Value);
        var result = await mediator.Send(new UpdateUserPreferencesCommand(
            userId.Value, dto.EmailNotifications, dto.SmsNotifications, dto.PushNotifications,
            dto.Language, dto.Currency, dto.NewsletterSubscribed), ct);
        return result.ToActionResult(
            data => Ok(ApiResponse<UserPreferencesDto>.Ok(data, "Preferences updated successfully")),
            MapError);
    }

    [HttpPost("change-password")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
    {
        var userId = currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        logger.LogInformation("Changing password for user {UserId}", userId.Value);

        if (dto.NewPassword != dto.ConfirmPassword)
            return BadRequest(ApiResponse<object>.Failure("New password and confirmation do not match", "PASSWORD_MISMATCH"));

        var result = await mediator.Send(new ChangePasswordCommand(userId.Value, dto.OldPassword, dto.NewPassword), ct);
        return result.ToActionResult(
            () => Ok(ApiResponse<object>.Ok(new object(), "Password changed successfully")),
            MapError);
    }

    [HttpDelete("addresses/{addressId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAddress([FromRoute] Guid addressId, CancellationToken ct)
    {
        var userId = currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        logger.LogInformation("Deleting address {AddressId} for user {UserId}", addressId, userId.Value);

        var result = await mediator.Send(new DeleteAddressCommand(userId.Value, addressId), ct);
        return result.ToActionResult(
            data => Ok(ApiResponse<UserProfileDto>.Ok(data, "Address deleted successfully")),
            MapError);
    }
}
