using ECommerce.Application.DTOs.Auth;
using ECommerce.Core.Results;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for authentication and authorization operations.
/// Uses Result&lt;T&gt; for predictable business failures (invalid credentials, expired tokens, etc).
/// Infrastructure failures (DB, network) still throw typed exceptions caught by middleware.
/// </summary>
public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<Result<AuthResponseDto>> RefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<Result<Unit>> VerifyEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default);
    Task<Result<string>> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default);
    Task<Result<Unit>> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);
    Task<Result<Unit>> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    string GenerateJwtToken(UserDto user);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
