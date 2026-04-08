namespace ECommerce.Contracts.DTOs.Auth;

public record UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string? Phone { get; init; }
    public string Role { get; init; } = null!;
    public string? AvatarUrl { get; init; }
}

