using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Users;

/// <summary>
/// User profile response DTO with full profile information.
/// </summary>
public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Phone { get; set; }
    public string Role { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
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
public class UserPreferencesDto
{
    public Guid UserId { get; set; }
    public bool EmailNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; } = false;
    public bool PushNotifications { get; set; } = true;
    public string Language { get; set; } = "en";
    public string Currency { get; set; } = "USD";
    public bool NewsletterSubscribed { get; set; } = false;
}

