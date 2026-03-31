using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Queries.GetUserPreferences;

public class GetUserPreferencesQueryHandler(IUserRepository users)
    : IRequestHandler<GetUserPreferencesQuery, Result<UserPreferencesDto>>
{
    public async Task<Result<UserPreferencesDto>> Handle(GetUserPreferencesQuery query, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(query.UserId, ct);
        if (user is null) return Result<UserPreferencesDto>.Fail(IdentityApplicationErrors.UserNotFound);

        // Return defaults — preferences are not yet persisted in the User aggregate
        return Result<UserPreferencesDto>.Ok(new UserPreferencesDto(
            EmailNotifications: true,
            SmsNotifications: false,
            PushNotifications: true,
            Language: "en",
            Currency: "USD",
            NewsletterSubscribed: false));
    }
}
