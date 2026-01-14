using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for authentication operations including registration and login.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="registerDto">The registration details including email, password, and name.</param>
    /// <returns>The newly created user information and authentication token.</returns>
    /// <response code="200">User registered successfully.</response>
    /// <response code="400">Invalid registration data or user already exists.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<AuthResponseDto>.Error("Validation failed", errors));
            }

            var result = await _authService.RegisterAsync(registerDto);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<AuthResponseDto>.Error(result.Message));
            }

            _logger.LogInformation("User registered successfully: {Email}", registerDto.Email);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "User registered successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, ApiResponse<AuthResponseDto>.Error("An error occurred during registration"));
        }
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="loginDto">The login credentials including email and password.</param>
    /// <returns>Authenticated user information and JWT token.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="400">Invalid credentials.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<AuthResponseDto>.Error("Validation failed", errors));
            }

            var result = await _authService.LoginAsync(loginDto);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<AuthResponseDto>.Error(result.Message));
            }

            _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Login successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, ApiResponse<AuthResponseDto>.Error("An error occurred during login"));
        }
    }

    /// <summary>
    /// Refreshes an expired JWT token.
    /// </summary>
    /// <param name="request">The refresh token request containing the expired token.</param>
    /// <returns>A new valid JWT token.</returns>
    /// <response code="200">Token refreshed successfully.</response>
    /// <response code="400">Invalid or expired token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<TokenResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TokenResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TokenResponseDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TokenResponseDto>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(ApiResponse<TokenResponseDto>.Error("Token is required"));
            }

            var newToken = await _authService.RefreshTokenAsync(request.Token);

            if (string.IsNullOrEmpty(newToken))
            {
                return BadRequest(ApiResponse<TokenResponseDto>.Error("Invalid or expired token"));
            }

            return Ok(ApiResponse<TokenResponseDto>.Ok(new TokenResponseDto { Token = newToken }, "Token refreshed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, ApiResponse<TokenResponseDto>.Error("An error occurred while refreshing token"));
        }
    }

    /// <summary>
    /// Verifies an email address using the provided verification token.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="token">The email verification token.</param>
    /// <returns>Verification result.</returns>
    /// <response code="200">Email verified successfully.</response>
    /// <response code="400">Invalid verification token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> VerifyEmail([FromQuery] Guid userId, [FromQuery] string token)
    {
        try
        {
            if (userId == Guid.Empty || string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(ApiResponse<object>.Error("User ID and token are required"));
            }

            var result = await _authService.VerifyEmailAsync(userId, token);

            if (!result)
            {
                return BadRequest(ApiResponse<object>.Error("Invalid or expired verification token"));
            }

            return Ok(ApiResponse<object>.Ok(null, "Email verified successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email");
            return StatusCode(500, ApiResponse<object>.Error("An error occurred during email verification"));
        }
    }

    /// <summary>
    /// Requests a password reset token.
    /// </summary>
    /// <param name="request">The password reset request containing the email.</param>
    /// <returns>Password reset token.</returns>
    /// <response code="200">Password reset token generated.</response>
    /// <response code="400">Invalid email.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ForgotPasswordResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ForgotPasswordResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ForgotPasswordResponseDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ForgotPasswordResponseDto>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(ApiResponse<ForgotPasswordResponseDto>.Error("Email is required"));
            }

            var token = await _authService.GeneratePasswordResetTokenAsync(request.Email);

            if (string.IsNullOrEmpty(token))
            {
                // Don't reveal if email exists for security reasons
                return Ok(ApiResponse<ForgotPasswordResponseDto>.Ok(
                    new ForgotPasswordResponseDto { Message = "If the email exists, a reset token has been sent" },
                    "Password reset token generated if email exists"));
            }

            return Ok(ApiResponse<ForgotPasswordResponseDto>.Ok(
                new ForgotPasswordResponseDto { Token = token },
                "Password reset token generated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating password reset token");
            return StatusCode(500, ApiResponse<ForgotPasswordResponseDto>.Error("An error occurred while generating reset token"));
        }
    }

    /// <summary>
    /// Resets the user's password using a reset token.
    /// </summary>
    /// <param name="request">The reset password request with email, token, and new password.</param>
    /// <returns>Password reset result.</returns>
    /// <response code="200">Password reset successfully.</response>
    /// <response code="400">Invalid token or password requirements not met.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(ApiResponse<object>.Error("Email, token, and new password are required"));
            }

            if (request.NewPassword.Length < 8)
            {
                return BadRequest(ApiResponse<object>.Error("Password must be at least 8 characters long"));
            }

            var result = await _authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);

            if (!result)
            {
                return BadRequest(ApiResponse<object>.Error("Invalid or expired reset token"));
            }

            return Ok(ApiResponse<object>.Ok(null, "Password reset successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password");
            return StatusCode(500, ApiResponse<object>.Error("An error occurred while resetting password"));
        }
    }

    /// <summary>
    /// Changes the user's password after verifying the old password.
    /// </summary>
    /// <param name="request">The change password request with old and new passwords.</param>
    /// <returns>Password change result.</returns>
    /// <response code="200">Password changed successfully.</response>
    /// <response code="400">Invalid current password or password requirements not met.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.Error("User is not authenticated"));
            }

            if (string.IsNullOrWhiteSpace(request.OldPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(ApiResponse<object>.Error("Old and new passwords are required"));
            }

            if (request.NewPassword.Length < 8)
            {
                return BadRequest(ApiResponse<object>.Error("Password must be at least 8 characters long"));
            }

            var result = await _authService.ChangePasswordAsync(Guid.Parse(userId), request.OldPassword, request.NewPassword);

            if (!result)
            {
                return BadRequest(ApiResponse<object>.Error("Invalid current password"));
            }

            _logger.LogInformation("Password changed for user: {UserId}", userId);
            return Ok(ApiResponse<object>.Ok(null, "Password changed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return StatusCode(500, ApiResponse<object>.Error("An error occurred while changing password"));
        }
    }
}

#region DTOs

public class RefreshTokenRequest
{
    public string Token { get; set; } = null!;
}

public class TokenResponseDto
{
    public string Token { get; set; } = null!;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = null!;
}

public class ForgotPasswordResponseDto
{
    public string? Token { get; set; }
    public string? Message { get; set; }
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = null!;
    public string Token { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

#endregion
