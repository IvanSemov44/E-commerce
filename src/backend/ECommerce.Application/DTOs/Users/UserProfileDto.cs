namespace ECommerce.Application.DTOs.Users;

/// <summary>
/// User profile response DTO with full profile information.
/// </summary>
public record UserProfileDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string? Phone { get; init; }
    public string Role { get; init; } = null!;
    public string? AvatarUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
