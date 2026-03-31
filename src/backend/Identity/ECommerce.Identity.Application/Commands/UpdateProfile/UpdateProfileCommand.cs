using ECommerce.Identity.Application.DTOs;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.UpdateProfile;

public record UpdateProfileCommand(Guid UserId, string FirstName, string LastName, string? PhoneNumber)
    : IRequest<Result<UserProfileDto>>, ITransactionalCommand;
