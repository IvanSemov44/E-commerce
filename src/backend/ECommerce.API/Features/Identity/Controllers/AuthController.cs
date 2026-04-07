using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using ECommerce.Identity.Application.Commands.ForgotPassword;
using ECommerce.Identity.Application.Commands.Login;
using ECommerce.Identity.Application.Commands.Logout;
using ECommerce.Identity.Application.Commands.RefreshToken;
using ECommerce.Identity.Application.Commands.Register;
using ECommerce.Identity.Application.Commands.ResetPassword;
using ECommerce.Identity.Application.Commands.VerifyEmail;
using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Queries.GetCurrentUser;
using ECommerce.Shopping.Application.Commands.MergeCart;
using ECommerce.Core.Constants;
using ECommerce.Core.Extensions;
using ECommerce.API.ActionFilters;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Features.Identity.Controllers;

/// <summary>
/// Controller for authentication operations including registration and login.
/// Uses httpOnly cookies for secure token storage (XSS protection).
/// Dispatches to Identity CQRS handlers via IMediator.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Auth")]
public class AuthController(
    IMediator mediator,
    ILogger<AuthController> logger,
    IConfiguration configuration,
    ICurrentUserService currentUser) : ControllerBase
{
    private void SetAuthCookies(string accessToken, string refreshToken)
    {
        var isProduction = !configuration.GetValue<bool>("IsDevelopment");
        var accessTokenExpiration = TimeSpan.FromMinutes(configuration.GetValue<int>("Jwt:ExpireMinutes", 60));
        var refreshTokenExpiration = TimeSpan.FromDays(7);
        var sameSite = isProduction ? SameSiteMode.None : SameSiteMode.Lax;
        var cookiePrefix = GetCookiePrefix();

        Response.Cookies.Append($"{cookiePrefix}_accessToken", accessToken, new CookieOptions
        {
            HttpOnly = true, Secure = isProduction, SameSite = sameSite,
            Expires = DateTimeOffset.UtcNow.Add(accessTokenExpiration), Path = "/"
        });
        Response.Cookies.Append($"{cookiePrefix}_refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true, Secure = isProduction, SameSite = sameSite,
            Expires = DateTimeOffset.UtcNow.Add(refreshTokenExpiration), Path = "/"
        });
    }

    private void ClearAuthCookies()
    {
        var cookiePrefix = GetCookiePrefix();
        Response.Cookies.Delete($"{cookiePrefix}_accessToken", new CookieOptions { Path = "/" });
        Response.Cookies.Delete($"{cookiePrefix}_refreshToken", new CookieOptions { Path = "/" });
    }

    private string GetCookiePrefix()
    {
        if (Request.Headers.TryGetValue("X-App-Origin", out var appOrigin))
        {
            var s = appOrigin.ToString().ToLowerInvariant();
            if (s.Contains("admin") || s.Contains("5177") || s.Contains("3001")) return "admin";
        }
        if (Request.Headers.TryGetValue("Origin", out var origin))
        {
            var s = origin.ToString().ToLowerInvariant();
            if (s.Contains("5177") || s.Contains("3001")) return "admin";
        }
        if (Request.Headers.TryGetValue("Referer", out var referer))
        {
            var s = referer.ToString().ToLowerInvariant();
            if (s.Contains("5177") || s.Contains("3001")) return "admin";
        }
        return "storefront";
    }

    private string? GetRefreshTokenFromCookie()
    {
        Request.Cookies.TryGetValue($"{GetCookiePrefix()}_refreshToken", out var token);
        return token;
    }

    private IActionResult MapError(ECommerce.SharedKernel.Results.DomainError error) => error.Code switch
    {
        "INVALID_CREDENTIALS" or "TOKEN_INVALID" or "TOKEN_REVOKED"
            => Unauthorized(ApiResponse<object>.Failure(error.Message, error.Code)),
        "EMAIL_TAKEN"
            => Conflict(ApiResponse<object>.Failure(error.Message, error.Code)),
        "USER_NOT_FOUND" or "ADDRESS_NOT_FOUND"
            => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),
        "VALIDATION_FAILED"
            => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code)),
        _ => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code))
    };

    /// <summary>Registers a new user account.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthLimit")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        var result = await mediator.Send(new RegisterCommand(dto.FirstName, dto.LastName, dto.Email, dto.Password), ct);
        if (!result.IsSuccess) return MapError(result.GetErrorOrThrow());

        var data = result.GetDataOrThrow();
        SetAuthCookies(data.AccessToken, data.RefreshToken);
        logger.LogInformation("User registered successfully: {Email}", dto.Email.MaskEmail());
        return Ok(ApiResponse<UserDto>.Ok(new UserDto
        {
            Id = data.UserId,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Role = "Customer"
        }, "User registered successfully"));
    }

    /// <summary>Authenticates a user and sets JWT tokens as httpOnly cookies.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthLimit")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var result = await mediator.Send(new LoginCommand(dto.Email, dto.Password), ct);
        if (!result.IsSuccess) return MapError(result.GetErrorOrThrow());

        var data = result.GetDataOrThrow();
        SetAuthCookies(data.AccessToken, data.RefreshToken);
        logger.LogInformation("User logged in successfully: {Email}", dto.Email.MaskEmail());

        // Merge session-based cart (if exists) with user cart
        var sessionId = currentUser.SessionId;
        if (sessionId is not null)
        {
            // Fire-and-forget merge; don't block login on merge failure
            _ = mediator.Send(new MergeCartCommand(data.UserId, sessionId), ct)
                .ContinueWith(_ => Response.Cookies.Delete("CartSession"), ct);
        }

        // Fetch user profile to include in response
        var profile = await mediator.Send(new GetCurrentUserQuery(data.UserId), ct);
        var userDto = profile.IsSuccess ? MapProfileToUserDto(profile.GetDataOrThrow()) : new UserDto
        {
            Id = data.UserId, Email = dto.Email, FirstName = "", LastName = "", Role = "Customer"
        };
        return Ok(ApiResponse<UserDto>.Ok(userDto, "Login successful"));
    }

    /// <summary>Refreshes an expired JWT token using the refresh token from httpOnly cookie.</summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(CancellationToken ct)
    {
        var refreshToken = GetRefreshTokenFromCookie();
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(ApiResponse<object>.Failure("Refresh token not found", "REFRESH_TOKEN_NOT_FOUND"));

        var result = await mediator.Send(new RefreshTokenCommand(refreshToken), ct);
        if (!result.IsSuccess) return MapError(result.GetErrorOrThrow());

        var data = result.GetDataOrThrow();
        SetAuthCookies(data.AccessToken, data.RefreshToken);

        var profile = await mediator.Send(new GetCurrentUserQuery(data.UserId), ct);
        var userDto = profile.IsSuccess ? MapProfileToUserDto(profile.GetDataOrThrow()) : new UserDto
        {
            Id = data.UserId, Email = "", FirstName = "", LastName = "", Role = "Customer"
        };
        return Ok(ApiResponse<UserDto>.Ok(userDto, "Token refreshed successfully"));
    }

    /// <summary>Logs out the user by clearing authentication cookies.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        ClearAuthCookies();
        logger.LogInformation("User logged out successfully");
        return Ok(ApiResponse<object>.Ok(new object(), "Logged out successfully"));
    }

    /// <summary>Gets the current authenticated user's profile.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(ApiResponse<object>.Failure("Invalid token", "INVALID_TOKEN"));

        var result = await mediator.Send(new GetCurrentUserQuery(userId), ct);
        if (!result.IsSuccess) return MapError(result.GetErrorOrThrow());

        return Ok(ApiResponse<UserProfileDto>.Ok(result.GetDataOrThrow(), "User profile retrieved successfully"));
    }

    /// <summary>Verifies a user's email address using the verification token.</summary>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto request, CancellationToken ct)
    {
        var result = await mediator.Send(new VerifyEmailCommand(request.UserId, request.Token), ct);
        if (!result.IsSuccess) return MapError(result.GetErrorOrThrow());

        logger.LogInformation("Email verified successfully for user {UserId}", request.UserId);
        return Ok(ApiResponse<object>.Ok(new object(), "Email verified successfully"));
    }

    /// <summary>Sends a password reset email to the user.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("PasswordResetLimit")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request, CancellationToken ct)
    {
        await mediator.Send(new ForgotPasswordCommand(request.Email), ct);
        logger.LogInformation("Password reset requested for {Email}", request.Email.MaskEmail());
        return Ok(ApiResponse<object>.Ok(new object(), "If an account with that email exists, a password reset link has been sent"));
    }

    /// <summary>Resets the user's password using a valid reset token.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request, CancellationToken ct)
    {
        var result = await mediator.Send(new ResetPasswordCommand(request.Email, request.Token, request.NewPassword), ct);
        if (!result.IsSuccess) return MapError(result.GetErrorOrThrow());

        logger.LogInformation("Password reset successfully for {Email}", request.Email.MaskEmail());
        return Ok(ApiResponse<object>.Ok(new object(), "Password reset successfully. You can now login with your new password"));
    }

    private static UserDto MapProfileToUserDto(UserProfileDto p) => new()
    {
        Id = p.Id, Email = p.Email, FirstName = p.FirstName, LastName = p.LastName,
        Phone = p.PhoneNumber, Role = p.Role, AvatarUrl = null
    };
}
