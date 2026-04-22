namespace ECommerce.Identity.Application.Commands.AddAddress;

public record AddAddressCommand(Guid UserId, string Street, string City, string Country, string? PostalCode)
    : IRequest<Result<UserProfileDto>>, ITransactionalCommand;
