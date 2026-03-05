namespace ECommerce.Application.DTOs.Auth;

/// <summary>
/// Response DTO for password reset initiation.
/// </summary>
public class ForgotPasswordResponseDto
{
    public string? Token { get; set; }
    public string? Message { get; set; }
}
