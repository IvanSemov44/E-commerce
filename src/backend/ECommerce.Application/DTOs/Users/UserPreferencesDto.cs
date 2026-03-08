namespace ECommerce.Application.DTOs.Users;

/// <summary>
/// DTO for user preferences.
/// </summary>
public record UserPreferencesDto
{
    public Guid UserId { get; init; }
    public bool EmailNotifications { get; init; } = true;
    public bool SmsNotifications { get; init; }
    public bool PushNotifications { get; init; } = true;
    public string Language { get; init; } = "en";
    public string Currency { get; init; } = "USD";
    public bool NewsletterSubscribed { get; init; }
}
