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
    /// Uses app-specific cookie names to prevent conflicts between admin and storefront.
    /// </summary>
    private void SetAuthCookies(string accessToken, string refreshToken)
    {
        var isProduction = !_configuration.GetValue<bool>("IsDevelopment");
        var accessTokenExpiration = TimeSpan.FromMinutes(_configuration.GetValue<int>("Jwt:ExpireMinutes", 60));
        var refreshTokenExpiration = TimeSpan.FromDays(7);

        // In production with cross-site setup (frontend and API on different domains),
        // we need SameSite=None to allow cookies to be sent cross-site
        // This requires Secure=true which is set in production
        var sameSite = isProduction ? SameSiteMode.None : SameSiteMode.Lax;

        // Determine cookie prefix based on the app origin (admin vs storefront)
        // This prevents cookie conflicts when both apps are used in the same browser
        var cookiePrefix = GetCookiePrefix();

        // Access token cookie (httpOnly for XSS protection)
        Response.Cookies.Append($"{cookiePrefix}_accessToken", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction, // Only send over HTTPS in production
            SameSite = sameSite,
            Expires = DateTimeOffset.UtcNow.Add(accessTokenExpiration),
            Path = "/"
        });

        // Refresh token cookie (httpOnly for XSS protection)
        Response.Cookies.Append($"{cookiePrefix}_refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = sameSite,
            Expires = DateTimeOffset.UtcNow.Add(refreshTokenExpiration),
            Path = "/"
        });
    }

    /// <summary>
    /// Clears authentication cookies on logout.
    /// </summary>
    private void ClearAuthCookies()
    {
        var cookiePrefix = GetCookiePrefix();
        Response.Cookies.Delete($"{cookiePrefix}_accessToken", new CookieOptions { Path = "/" });
        Response.Cookies.Delete($"{cookiePrefix}_refreshToken", new CookieOptions { Path = "/" });
    }

    /// <summary>
    /// Gets the cookie prefix based on the requesting app.
    /// Admin panel uses "admin" prefix, storefront uses "storefront" prefix.
    /// This prevents cookie conflicts when both apps are used in the same browser.
    /// </summary>
    private string GetCookiePrefix()
    {
        // Check for custom header that identifies the app
        if (Request.Headers.TryGetValue("X-App-Origin", out var appOrigin))
        {
            var appOriginStr = appOrigin.ToString().ToLowerInvariant();
            if (appOriginStr.Contains("admin") || appOriginStr.Contains("5177") || appOriginStr.Contains("3001"))
            {
                return "admin";
            }
        }

        // Check Origin header
        if (Request.Headers.TryGetValue("Origin", out var originHeader))
        {
            var originStr = originHeader.ToString().ToLowerInvariant();
            if (originStr.Contains("5177") || originStr.Contains("3001"))
            {
                return "admin";
            }
        }

        // Check Referer header as fallback
        if (Request.Headers.TryGetValue("Referer", out var referer))
        {
            var refererStr = referer.ToString().ToLowerInvariant();
            if (refererStr.Contains("5177") || refererStr.Contains("3001"))
            {
                return "admin";
            }
        }

        // Default to storefront
        return "storefront";
    }

    /// <summary>
    /// Gets the access token from the appropriate cookie based on the requesting app.
    /// </summary>
    private string? GetAccessTokenFromCookie()
    {
        var cookiePrefix = GetCookiePrefix();
        Request.Cookies.TryGetValue($"{cookiePrefix}_accessToken", out var token);
        return token;
    }

    /// <summary>
    /// Gets the refresh token from the appropriate cookie based on the requesting app.
    /// </summary>
    private string? GetRefreshTokenFromCookie()
    {
        var cookiePrefix = GetCookiePrefix();
        Request.Cookies.TryGetValue($"{cookiePrefix}_refreshToken", out var token);
        return token;
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="registerDto">The registration details including email, password, and name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created user information (tokens set as httpOnly cookies).</returns>
    /// <response code="200">User registered successfully.</response>
    /// <response code="409">User already exists with that email.</response>
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
        
        return result.Match(
            onSuccess: authResponse =>
            {
                // Set httpOnly cookies instead of returning tokens in body
                SetAuthCookies(authResponse.Token!, authResponse.RefreshToken!);
                _logger.LogInformation("User registered successfully: {Email}", registerDto.Email);
                return Ok(ApiResponse<UserDto>.Ok(authResponse.User!, "User registered successfully"));
            },
            onFailure: failure =>
            {
                if (failure is ECommerce.Core.Results.Result<AuthResponseDto>.Failure f)
                {
                    _logger.LogWarning("Registration failed for {Email}: {Code} - {Message}",
                        registerDto.Email, f.Code, f.Message);
                    
                    return f.Code switch
                    {
                        "DUPLICATE_EMAIL" => Conflict(ApiResponse<object>.Failure(f.Message, f.Code)),
                        _ => BadRequest(ApiResponse<object>.Failure(f.Message, f.Code))
                    };
                }
                return StatusCode(500, ApiResponse<object>.Failure("Registration failed", "REGISTRATION_ERROR"));
            }
        );
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
        
        return result.Match(
            onSuccess: authResponse =>
            {
                // Set httpOnly cookies instead of returning tokens in body
                SetAuthCookies(authResponse.Token!, authResponse.RefreshToken!);
                _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);
                return Ok(ApiResponse<UserDto>.Ok(authResponse.User!, "Login successful"));
            },
            onFailure: failure =>
            {
                if (failure is ECommerce.Core.Results.Result<AuthResponseDto>.Failure f)
                {
                    _logger.LogWarning("Login failed for {Email}: {Code}", loginDto.Email, f.Code);
                    return Unauthorized(ApiResponse<object>.Failure(f.Message, f.Code));
                }
                return StatusCode(500, ApiResponse<object>.Failure("Login failed", "LOGIN_ERROR"));
            }
        );
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
        // Get refresh token from httpOnly cookie (app-specific)
        var refreshToken = GetRefreshTokenFromCookie();
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(ApiResponse<object>.Failure("Refresh token not found", "REFRESH_TOKEN_NOT_FOUND"));
        }

        var result = await _authService.RefreshTokenAsync(refreshToken, cancellationToken: cancellationToken);
        
        return result.Match(
            onSuccess: authResponse =>
            {
                // Set new tokens as httpOnly cookies
                SetAuthCookies(authResponse.Token!, authResponse.RefreshToken!);
                return Ok(ApiResponse<UserDto>.Ok(authResponse.User!, "Token refreshed successfully"));
            },
            onFailure: failure =>
            {
                if (failure is ECommerce.Core.Results.Result<AuthResponseDto>.Failure f)
                {
                    _logger.LogWarning("Token refresh failed: {Code}", f.Code);
                    return Unauthorized(ApiResponse<object>.Failure(f.Message, f.Code));
                }
                return StatusCode(500, ApiResponse<object>.Failure("Token refresh failed", "REFRESH_ERROR"));
            }
        );
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
            return Unauthorized(ApiResponse<object>.Failure("Invalid token", "INVALID_TOKEN"));
        }

        var user = await _authService.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return Unauthorized(ApiResponse<object>.Failure("User not found", "USER_NOT_FOUND"));
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
        var result = await _authService.VerifyEmailAsync(request.UserId, request.Token, cancellationToken: cancellationToken);
        
        return result.Match(
            onSuccess: _ =>
            {
                _logger.LogInformation("Email verified successfully for user {UserId}", request.UserId);
                return Ok(ApiResponse<object>.Ok(new object(), "Email verified successfully"));
            },
            onFailure: failure =>
            {
                if (failure is ECommerce.Core.Results.Result<ECommerce.Core.Results.Unit>.Failure f)
                {
                    _logger.LogWarning("Email verification failed for user {UserId}: {Code}", request.UserId, f.Code);
                    
                    return f.Code switch
                    {
                        "USER_NOT_FOUND" => NotFound(ApiResponse<object>.Failure(f.Message, f.Code)),
                        "INVALID_TOKEN" => Unauthorized(ApiResponse<object>.Failure(f.Message, f.Code)),
                        _ => BadRequest(ApiResponse<object>.Failure(f.Message, f.Code))
                    };
                }
                return StatusCode(500, ApiResponse<object>.Failure("Email verification failed", "VERIFICATION_ERROR"));
            }
        );
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
        var result = await _authService.GeneratePasswordResetTokenAsync(request.Email, cancellationToken: cancellationToken);

        // Always return success for security reasons (don't reveal if email exists)
        // The service already handles logging of non-existent emails
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
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword, cancellationToken: cancellationToken);
        
        return result.Match(
            onSuccess: _ =>
            {
                _logger.LogInformation("Password reset successfully for {Email}", request.Email);
                return Ok(ApiResponse<object>.Ok(new object(), "Password reset successfully. You can now login with your new password"));
            },
            onFailure: failure =>
            {
                if (failure is ECommerce.Core.Results.Result<ECommerce.Core.Results.Unit>.Failure f)
                {
                    _logger.LogWarning("Password reset failed for {Email}: {Code}", request.Email, f.Code);
                    return f.Code switch
                    {
                        "USER_NOT_FOUND" => NotFound(ApiResponse<object>.Failure(f.Message, f.Code)),
                        "INVALID_TOKEN" => Unauthorized(ApiResponse<object>.Failure(f.Message, f.Code)),
                        _ => BadRequest(ApiResponse<object>.Failure(f.Message, f.Code))
                    };
                }
                return StatusCode(500, ApiResponse<object>.Failure("Password reset failed", "RESET_ERROR"));
            }
        );    }
}