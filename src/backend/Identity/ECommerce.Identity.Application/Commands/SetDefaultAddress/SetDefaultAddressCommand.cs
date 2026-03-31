using ECommerce.Identity.Application.DTOs;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.SetDefaultAddress;

public record SetDefaultAddressCommand(Guid UserId, Guid AddressId)
    : IRequest<Result<UserProfileDto>>, ITransactionalCommand;
