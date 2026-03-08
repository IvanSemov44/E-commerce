namespace ECommerce.Application.DTOs.Users;

public static class UserPreferencesDtoExtensions
{
    public static UserPreferencesDto GetDefaultPreferences(Guid userId) =>
        new() { UserId = userId };
}
