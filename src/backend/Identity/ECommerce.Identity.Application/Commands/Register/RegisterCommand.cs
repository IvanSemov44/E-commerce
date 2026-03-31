using ECommerce.Identity.Application.DTOs;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.Register;

public record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password
) : IRequest<Result<AuthTokenDto>>, ITransactionalCommand;
