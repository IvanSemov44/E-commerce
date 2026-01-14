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
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(ApiResponse<AuthResponseDto>.Error("Token is required"));
            }

            var result = await _authService.RefreshTokenAsync(request.Token);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<AuthResponseDto>.Error("Invalid or expired token"));
            }

            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Token refreshed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, ApiResponse<AuthResponseDto>.Error("An error occurred while refreshing token"));
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
