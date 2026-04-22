namespace ECommerce.Identity.Application.Commands.SetDefaultAddress;

public record SetDefaultAddressCommand(Guid UserId, Guid AddressId)
    : IRequest<Result<UserProfileDto>>, ITransactionalCommand;
