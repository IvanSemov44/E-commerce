namespace ECommerce.Identity.Application.Commands.UpdateProfile;

public record UpdateProfileCommand(Guid UserId, string FirstName, string LastName, string? PhoneNumber)
    : IRequest<Result<UserProfileDto>>, ITransactionalCommand;
