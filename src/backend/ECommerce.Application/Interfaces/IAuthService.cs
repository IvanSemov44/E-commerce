using ECommerce.Application.DTOs.Auth;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for authentication and authorization operations.
/// </summary>
public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(string token);
    Task<bool> ValidateTokenAsync(string token);
    Task VerifyEmailAsync(Guid userId, string token);
    Task<string> GeneratePasswordResetTokenAsync(string email);
    Task ResetPasswordAsync(string email, string token, string newPassword);
    Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);
    string GenerateJwtToken(UserDto user);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
