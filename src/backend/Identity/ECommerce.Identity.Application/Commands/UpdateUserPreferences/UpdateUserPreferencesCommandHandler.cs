namespace ECommerce.Identity.Application.Commands.UpdateUserPreferences;

public class UpdateUserPreferencesCommandHandler(
    IUserRepository users
) : IRequestHandler<UpdateUserPreferencesCommand, Result<UserPreferencesDto>>
{
    public async Task<Result<UserPreferencesDto>> Handle(UpdateUserPreferencesCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(command.UserId, ct);
        if (user is null)
            return Result<UserPreferencesDto>.Fail(IdentityApplicationErrors.UserNotFound);

        // Preferences are not yet persisted in the User aggregate — return the requested values
        var dto = new UserPreferencesDto(
            command.EmailNotifications,
            command.SmsNotifications,
            command.PushNotifications,
            command.Language,
            command.Currency,
            command.NewsletterSubscribed);
        return Result<UserPreferencesDto>.Ok(dto);
    }
}
