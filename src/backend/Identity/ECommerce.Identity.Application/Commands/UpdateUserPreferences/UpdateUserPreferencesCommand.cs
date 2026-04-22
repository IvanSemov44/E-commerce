namespace ECommerce.Identity.Application.Commands.UpdateUserPreferences;

public record UpdateUserPreferencesCommand(
    Guid UserId,
    bool EmailNotifications,
    bool SmsNotifications,
    bool PushNotifications,
    string Language,
    string Currency,
    bool NewsletterSubscribed
) : IRequest<Result<UserPreferencesDto>>, ITransactionalCommand;
