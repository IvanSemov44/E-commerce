using ECommerce.Identity.Application.DTOs;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.DeleteAddress;

public record DeleteAddressCommand(Guid UserId, Guid AddressId)
    : IRequest<Result<UserProfileDto>>, ITransactionalCommand;
