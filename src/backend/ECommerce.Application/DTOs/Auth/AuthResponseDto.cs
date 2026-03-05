namespace ECommerce.Application.DTOs.Auth;

public class AuthResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public UserDto? User { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
}
