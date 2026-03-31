using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.UpdateUserPreferences;

public class UpdateUserPreferencesCommandHandler(
    IUserRepository users,
    IUnitOfWork uow
) : IRequestHandler<UpdateUserPreferencesCommand, Result<UserPreferencesDto>>
{
    public async Task<Result<UserPreferencesDto>> Handle(UpdateUserPreferencesCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(command.UserId, ct);
        if (user is null) return Result<UserPreferencesDto>.Fail(IdentityApplicationErrors.UserNotFound);

        // Preferences are not yet persisted in the User aggregate — return the requested values
        var dto = new UserPreferencesDto(
            command.EmailNotifications,
            command.SmsNotifications,
            command.PushNotifications,
            command.Language,
            command.Currency,
            command.NewsletterSubscribed);

        await uow.SaveChangesAsync(ct);
        return Result<UserPreferencesDto>.Ok(dto);
    }
}
