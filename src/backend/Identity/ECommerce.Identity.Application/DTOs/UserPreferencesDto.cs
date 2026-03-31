namespace ECommerce.Identity.Application.DTOs;

public record UserPreferencesDto(
    bool EmailNotifications,
    bool SmsNotifications,
    bool PushNotifications,
    string Language,
    string Currency,
    bool NewsletterSubscribed
);
