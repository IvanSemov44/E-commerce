namespace ECommerce.Application.DTOs.Auth;

/// <summary>
/// Response DTO containing a new authentication token.
/// </summary>
public class TokenResponseDto
{
    public string Token { get; set; } = null!;
}
