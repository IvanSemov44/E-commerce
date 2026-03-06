namespace ECommerce.Application.DTOs.Auth;

/// <summary>
/// Response DTO for password reset initiation.
/// </summary>
public record ForgotPasswordResponseDto
{
    public string? Token { get; init; }
    public string? Message { get; init; }
}
