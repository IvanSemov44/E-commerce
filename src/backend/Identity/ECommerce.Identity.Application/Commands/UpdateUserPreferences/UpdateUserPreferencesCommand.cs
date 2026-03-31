using ECommerce.Identity.Application.DTOs;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

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
