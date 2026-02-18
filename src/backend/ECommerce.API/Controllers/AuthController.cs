using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using ECommerce.API.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for authentication operations including registration and login.
/// Uses httpOnly cookies for secure token storage (XSS protection).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, ILogger<AuthController> logger, IConfiguration configuration)
    {
        _authService = authService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Sets authentication tokens as httpOnly cookies.
    /// </summary>
    private void SetAuthCookies(string accessToken, string refreshToken)
    {
        var isProduction = !_configuration.GetValue<bool>("IsDevelopment");
        var accessTokenExpiration = TimeSpan.FromMinutes(_configuration.GetValue<int>("Jwt:ExpireMinutes", 60));
        var refreshTokenExpiration = TimeSpan.FromDays(7);

        // Access token cookie (httpOnly for XSS protection)
        Response.Cookies.Append("accessToken", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction, // Only send over HTTPS in production
            SameSite = isProduction ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.Add(accessTokenExpiration),
            Path = "/"
        });

        // Refresh token cookie (httpOnly for XSS protection)
        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = isProduction ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.Add(refreshTokenExpiration),
            Path = "/"
        });
    }

    /// <summary>
    /// Clears authentication cookies on logout.
    /// </summary>
    private void ClearAuthCookies()
    {
        Response.Cookies.Delete("accessToken", new CookieOptions { Path = "/" });
        Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/" });
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="registerDto">The registration details including email, password, and name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created user information (tokens set as httpOnly cookies).</returns>
    /// <response code="200">User registered successfully.</response>
    /// <response code="400">Invalid registration data or user already exists.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthLimit")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserDto>>> Register([FromBody] RegisterDto registerDto, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(registerDto, cancellationToken: cancellationToken);
        
        // Set httpOnly cookies instead of returning tokens in body
        SetAuthCookies(result.Token!, result.RefreshToken!);
        
        _logger.LogInformation("User registered successfully: {Email}", registerDto.Email);
        return Ok(ApiResponse<UserDto>.Ok(result.User!, "User registered successfully"));
    }

    /// <summary>
    /// Authenticates a user and sets JWT tokens as httpOnly cookies.
    /// </summary>
    /// <param name="loginDto">The login credentials including email and password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authenticated user information (tokens set as httpOnly cookies).</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthLimit")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserDto>>> Login([FromBody] LoginDto loginDto, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(loginDto, cancellationToken: cancellationToken);
        
        // Set httpOnly cookies instead of returning tokens in body
        SetAuthCookies(result.Token!, result.RefreshToken!);
        
        _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);
        return Ok(ApiResponse<UserDto>.Ok(result.User!, "Login successful"));
    }

    /// <summary>
    /// Refreshes an expired JWT token using the refresh token from httpOnly cookie.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new valid JWT token (set as httpOnly cookie).</returns>
    /// <response code="200">Token refreshed successfully.</response>
    /// <response code="401">Invalid or expired token.</response>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserDto>>> RefreshToken(CancellationToken cancellationToken)
    {
        // Get refresh token from httpOnly cookie
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(ApiResponse<object>.Error("Refresh token not found"));
        }

        var result = await _authService.RefreshTokenAsync(refreshToken, cancellationToken: cancellationToken);
        
        // Set new tokens as httpOnly cookies
        SetAuthCookies(result.Token!, result.RefreshToken!);
        
        return Ok(ApiResponse<UserDto>.Ok(result.User!, "Token refreshed successfully"));
    }

    /// <summary>
    /// Logs out the user by clearing authentication cookies.
    /// </summary>
    /// <returns>Success message.</returns>
    /// <response code="200">Logged out successfully.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        ClearAuthCookies();
        _logger.LogInformation("User logged out successfully");
        return Ok(ApiResponse<object>.Ok(new object(), "Logged out successfully"));
    }

    /// <summary>
    /// Gets the current authenticated user's profile.
    /// </summary>
    /// <returns>Current user profile if authenticated.</returns>
    /// <response code="200">User profile retrieved.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser(CancellationToken cancellationToken)
    {
        // Get user ID from the JWT token claims
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<object>.Error("Invalid token"));
        }

        var user = await _authService.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return Unauthorized(ApiResponse<object>.Error("User not found"));
        }

        return Ok(ApiResponse<UserDto>.Ok(user, "User profile retrieved successfully"));
    }

    /// <summary>
    /// Verifies a user's email address using the verification token.
    /// </summary>
    /// <param name="request">The email verification request containing userId and token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification result.</returns>
    /// <response code="200">Email verified successfully.</response>
    /// <response code="401">Invalid or expired verification token.</response>
    /// <response code="404">User not found.</response>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        await _authService.VerifyEmailAsync(request.UserId, request.Token, cancellationToken: cancellationToken);
        _logger.LogInformation("Email verified successfully for user {UserId}", request.UserId);
        return Ok(ApiResponse<object>.Ok(new object(), "Email verified successfully"));
    }

    /// <summary>
    /// Sends a password reset email to the user.
    /// </summary>
    /// <param name="request">The forgot password request containing the user's email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success message if email exists (security: always return success).</returns>
    /// <response code="200">Password reset email sent (or user not found, but we don't reveal that).</response>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("PasswordResetLimit")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.GeneratePasswordResetTokenAsync(request.Email, cancellationToken: cancellationToken);

        // Always return success for security reasons (don't reveal if email exists)
        _logger.LogInformation("Password reset requested for {Email}", request.Email);
        return Ok(ApiResponse<object>.Ok(new object(), "If an account with that email exists, a password reset link has been sent"));
    }

    /// <summary>
    /// Resets the user's password using a valid reset token.
    /// </summary>
    /// <param name="request">The reset password request containing email, token, and new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Password reset result.</returns>
    /// <response code="200">Password reset successfully.</response>
    /// <response code="401">Invalid or expired reset token.</response>
    /// <response code="404">User not found.</response>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword, cancellationToken: cancellationToken);
        _logger.LogInformation("Password reset successfully for {Email}", request.Email);
        return Ok(ApiResponse<object>.Ok(new object(), "Password reset successfully. You can now login with your new password"));
    }

}


