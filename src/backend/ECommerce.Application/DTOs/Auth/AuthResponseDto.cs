namespace ECommerce.Application.DTOs.Auth;

public record AuthResponseDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = null!;
    public UserDto? User { get; init; }
    public string? Token { get; init; }
    public string? RefreshToken { get; init; }
}
