namespace ECommerce.Identity.Application.Commands.DeleteAddress;

public record DeleteAddressCommand(Guid UserId, Guid AddressId)
    : IRequest<Result<UserProfileDto>>, ITransactionalCommand;
