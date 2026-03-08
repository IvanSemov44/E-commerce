namespace ECommerce.Application.DTOs.Auth;

/// <summary>
/// Response DTO containing a new authentication token.
/// </summary>
public record TokenResponseDto
{
    public string Token { get; init; } = null!;
}
