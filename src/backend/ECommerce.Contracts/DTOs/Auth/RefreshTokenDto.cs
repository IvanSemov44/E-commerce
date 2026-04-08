namespace ECommerce.Contracts.DTOs.Auth;

/// <summary>
/// Request DTO for refreshing authentication tokens.
/// </summary>
public class RefreshTokenDto
{
    public string Token { get; set; } = null!;
}

