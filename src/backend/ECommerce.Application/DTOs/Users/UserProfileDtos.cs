using System.ComponentModel.DataAnnotations;

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

/// <summary>
/// DTO for updating user profile information.
/// </summary>
public class UpdateProfileDto
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 50 characters")]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 50 characters")]
    public string LastName { get; set; } = null!;

    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Phone number must not exceed 20 characters")]
    public string? Phone { get; set; }

    [Url(ErrorMessage = "Invalid URL format")]
    [StringLength(500, ErrorMessage = "Avatar URL must not exceed 500 characters")]
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// DTO for user preferences.
/// </summary>
public record UserPreferencesDto
{
    public Guid UserId { get; init; }
    public bool EmailNotifications { get; init; } = true;
    public bool SmsNotifications { get; init; } = false;
    public bool PushNotifications { get; init; } = true;
    public string Language { get; init; } = "en";
    public string Currency { get; init; } = "USD";
    public bool NewsletterSubscribed { get; init; } = false;
}

public static class UserPreferencesDtoExtensions
{
    public static UserPreferencesDto GetDefaultPreferences(Guid userId) =>
        new() { UserId = userId };
}

/// <summary>
/// DTO for changing user password.
/// </summary>
public class ChangePasswordDto
{
    [Required(ErrorMessage = "Old password is required")]
    public string OldPassword { get; set; } = null!;

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "New password must be between 8 and 100 characters")]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Password confirmation is required")]
    public string ConfirmPassword { get; set; } = null!;
}


