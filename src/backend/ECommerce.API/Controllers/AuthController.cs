using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
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
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterDto registerDto)
    {
        var result = await _authService.RegisterAsync(registerDto);
        _logger.LogInformation("User registered successfully: {Email}", registerDto.Email);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "User registered successfully"));
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="loginDto">The login credentials including email and password.</param>
    /// <returns>Authenticated user information and JWT token.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto loginDto)
    {
        var result = await _authService.LoginAsync(loginDto);
        _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Login successful"));
    }

    /// <summary>
    /// Refreshes an expired JWT token.
    /// </summary>
    /// <param name="request">The refresh token request containing the expired token.</param>
    /// <returns>A new valid JWT token.</returns>
    /// <response code="200">Token refreshed successfully.</response>
    /// <response code="401">Invalid or expired token.</response>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.Token);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Token refreshed successfully"));
    }

    /// <summary>
    /// Verifies a user's email address using the verification token.
    /// </summary>
    /// <param name="request">The email verification request containing userId and token.</param>
    /// <returns>Verification result.</returns>
    /// <response code="200">Email verified successfully.</response>
    /// <response code="401">Invalid or expired verification token.</response>
    /// <response code="404">User not found.</response>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        await _authService.VerifyEmailAsync(request.UserId, request.Token);
        _logger.LogInformation("Email verified successfully for user {UserId}", request.UserId);
        return Ok(ApiResponse<object>.Ok(new object(), "Email verified successfully"));
    }

    /// <summary>
    /// Sends a password reset email to the user.
    /// </summary>
    /// <param name="request">The forgot password request containing the user's email.</param>
    /// <returns>Success message if email exists (security: always return success).</returns>
    /// <response code="200">Password reset email sent (or user not found, but we don't reveal that).</response>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.GeneratePasswordResetTokenAsync(request.Email);

        // Always return success for security reasons (don't reveal if email exists)
        _logger.LogInformation("Password reset requested for {Email}", request.Email);
        return Ok(ApiResponse<object>.Ok(new object(), "If an account with that email exists, a password reset link has been sent"));
    }

    /// <summary>
    /// Resets the user's password using a valid reset token.
    /// </summary>
    /// <param name="request">The reset password request containing email, token, and new password.</param>
    /// <returns>Password reset result.</returns>
    /// <response code="200">Password reset successfully.</response>
    /// <response code="401">Invalid or expired reset token.</response>
    /// <response code="404">User not found.</response>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
        _logger.LogInformation("Password reset successfully for {Email}", request.Email);
        return Ok(ApiResponse<object>.Ok(new object(), "Password reset successfully. You can now login with your new password"));
    }

}

