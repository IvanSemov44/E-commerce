namespace ECommerce.Application.DTOs.Auth;

/// <summary>
/// Request DTO for changing password (authenticated users).
/// </summary>
public class ChangePasswordDto
{
    public string OldPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
