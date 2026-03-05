namespace ECommerce.Application.DTOs.Auth;

/// <summary>
/// Request DTO for initiating password reset.
/// </summary>
public class ForgotPasswordDto
{
    public string Email { get; set; } = null!;
}
