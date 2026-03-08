namespace ECommerce.Application.DTOs.Auth;

/// <summary>
/// Request DTO for resetting password with token.
/// </summary>
public class ResetPasswordDto
{
    public string Email { get; set; } = null!;
    public string Token { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
