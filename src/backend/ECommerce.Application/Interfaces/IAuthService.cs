using ECommerce.Application.DTOs.Auth;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for authentication and authorization operations.
/// </summary>
public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> RefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
    Task VerifyEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default);
    Task<string> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    string GenerateJwtToken(UserDto user);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
