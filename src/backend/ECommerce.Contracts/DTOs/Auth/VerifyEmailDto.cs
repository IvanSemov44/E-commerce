namespace ECommerce.Contracts.DTOs.Auth;

/// <summary>
/// Request DTO for email verification.
/// </summary>
public class VerifyEmailDto
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = null!;
}

