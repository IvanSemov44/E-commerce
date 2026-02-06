namespace ECommerce.Application.DTOs.Auth;

public class RegisterDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
}

public class LoginDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class AuthResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public UserDto? User { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Phone { get; set; }
    public string Role { get; set; } = null!;
    public string? AvatarUrl { get; set; }
}
